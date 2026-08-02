import { Columns3, List, Plus, Search, SlidersHorizontal, Sparkles } from 'lucide-react';
import { useMemo, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useAppContext } from '../../app/AppProviders';
import { useProjects, useTasks, useTransitionTask } from '../../api/dataSource';
import { PageHeader } from '../../components/PageHeader';
import { Button } from '../../components/ui/Button';
import { EmptyState, LoadingSkeleton, ProblemState } from '../../components/ui/States';
import { canCreateTask } from '../../lib/taskRules';
import type { TaskPriority, TaskStatus } from '../../types/domain';
import { CreateTaskDialog } from './CreateTaskDialog';
import { AiSuggestionReview } from '../ai/AiSuggestionReview';
import { TaskDetailDrawer } from './TaskDetailDrawer';
import { TaskKanban } from './TaskKanban';
import { TaskTable } from './TaskTable';

export function TasksPage() {
  const { workspaceId = '1', projectId = '1', taskId } = useParams();
  const pid = Number(projectId);
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState<TaskStatus | 'all'>('all');
  const [priority, setPriority] = useState<TaskPriority | 'all'>('all');
  const [createOpen, setCreateOpen] = useState(false);
  const [aiOpen, setAiOpen] = useState(false);
  const { user } = useAppContext();
  const tasks = useTasks(pid);
  const project = useProjects(Number(workspaceId)).data?.items.find((item) => item.id === pid);
  const mutation = useTransitionTask(pid);
  const view = searchParams.get('view') === 'kanban' ? 'kanban' : 'list';
  const filtered = useMemo(() => (tasks.data?.items ?? []).filter((task) => {
    if (query && !`${task.id} ${task.title}`.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr'))) return false;
    if (status !== 'all' && task.status !== status) return false;
    return !(priority !== 'all' && task.priority !== priority);
  }), [priority, query, status, tasks.data?.items]);
  const selected = tasks.data?.items.find((task) => task.id === Number(taskId)) ?? null;
  const href = (task: { id: number }) => `/w/${workspaceId}/projects/${pid}/tasks/${task.id}${view === 'kanban' ? '?view=kanban' : ''}`;
  const transition = (task: NonNullable<typeof selected>, next: TaskStatus) => mutation.mutate({ task, status: next });
  if (tasks.isLoading) return <LoadingSkeleton rows={9} />;
  if (tasks.error) return <ProblemState error={tasks.error} onRetry={() => void tasks.refetch()} />;
  return (
    <div className="page page--tasks">
      {project?.status === 'Archived' ? <div className="archive-banner">Bu proje arşivlenmiştir. Görevler salt okunur.</div> : null}
      <PageHeader title="Görevler" description={`${project?.name ?? 'Proje'} · ${tasks.data?.totalCount ?? 0} görev`} actions={<><Button icon={<Sparkles />} className="ai-button" onClick={() => setAiOpen(true)}>AI ile tasarla</Button>{canCreateTask(user.projectRole, project?.status === 'Archived') ? <Button variant="primary" icon={<Plus />} onClick={() => setCreateOpen(true)}>Yeni görev</Button> : null}</>} />
      <div className="task-controls">
        <label className="search-field"><Search /><span className="sr-only">Görev ara</span><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Başlık veya ID ara…" /></label>
        <label className="compact-select"><SlidersHorizontal /><span className="sr-only">Durum filtresi</span><select value={status} onChange={(event) => setStatus(event.target.value as TaskStatus | 'all')}><option value="all">Tüm durumlar</option><option value="Todo">Yapılacak</option><option value="InProgress">Devam ediyor</option><option value="InReview">İncelemede</option><option value="Done">Tamamlandı</option><option value="Cancelled">İptal edildi</option></select></label>
        <label className="compact-select"><span className="sr-only">Öncelik filtresi</span><select value={priority} onChange={(event) => setPriority(event.target.value as TaskPriority | 'all')}><option value="all">Tüm öncelikler</option><option value="Low">Düşük</option><option value="Medium">Orta</option><option value="High">Yüksek</option><option value="Critical">Kritik</option></select></label>
        <span className="result-count">{filtered.length} / {tasks.data?.totalCount}</span>
        <div className="view-switch" aria-label="Görev görünümü"><button className={view === 'list' ? 'is-active' : ''} onClick={() => setSearchParams({ view: 'list' })}><List />Liste</button><button className={view === 'kanban' ? 'is-active' : ''} onClick={() => setSearchParams({ view: 'kanban' })}><Columns3 />Kanban</button></div>
      </div>
      {!filtered.length ? <EmptyState title="Görev bulunamadı" description="Filtreleri temizleyin veya yeni bir görev oluşturun." action={{ label: 'Filtreleri temizle', onClick: () => { setQuery(''); setStatus('all'); setPriority('all'); } }} /> : view === 'list' ? <TaskTable tasks={filtered} getHref={href} /> : <TaskKanban tasks={filtered} user={user} projectArchived={project?.status === 'Archived'} getHref={href} onTransition={transition} />}
      <TaskDetailDrawer task={selected} project={project} open={Boolean(selected)} onClose={() => navigate(`/w/${workspaceId}/projects/${pid}/tasks${view === 'kanban' ? '?view=kanban' : ''}`)} onTransition={(next) => selected && transition(selected, next)} pending={mutation.isPending} />
      <CreateTaskDialog open={createOpen} onClose={() => setCreateOpen(false)} projectId={pid} />
      <AiSuggestionReview open={aiOpen} onClose={() => setAiOpen(false)} projectId={pid} />
    </div>
  );
}
