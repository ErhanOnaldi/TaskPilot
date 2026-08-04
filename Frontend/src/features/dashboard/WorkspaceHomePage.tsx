import { ArrowRight, Bot, CalendarClock, FolderKanban, Plus, Sparkles } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { useProjects, useTasks, useWorkspace } from '../../api/dataSource';
import { PageHeader } from '../../components/PageHeader';
import { LoadingSkeleton, ProblemState } from '../../components/ui/States';
import { PriorityBadge, StatusBadge } from '../../components/ui/Badge';
import { formatRelativeDue } from '../../lib/formatters';
import { useAppContext } from '../../app/AppProviders';

export function WorkspaceHomePage() {
  const { workspaceId = '1' } = useParams();
  const wid = Number(workspaceId);
  const { user } = useAppContext();
  const workspace = useWorkspace(wid);
  const projects = useProjects(wid);
  const firstProject = projects.data?.items.find((project) => project.status === 'Active');
  const tasks = useTasks(firstProject?.id ?? 0);
  if (workspace.isLoading || projects.isLoading) return <LoadingSkeleton rows={8} />;
  if (workspace.error || projects.error) return <ProblemState error={workspace.error ?? projects.error} onRetry={() => { void workspace.refetch(); void projects.refetch(); }} />;
  const openTasks = (tasks.data?.items ?? []).filter((task) => task.assignedUserId === user.id && !['Done', 'Cancelled'].includes(task.status));
  return (
    <div className="page page--home">
      <PageHeader eyebrow={formatToday()} title={`Tekrar hoş geldiniz, ${user.email.split('@')[0] || 'ekip'}.`} description="Projelerinizdeki riskleri ve yaklaşan işleri tek bakışta görün." />
      <section className="quick-actions" aria-label="Hızlı aksiyonlar">
        <Link to={`/w/${wid}/projects`}><FolderKanban /><span><strong>Projeleri aç</strong><small>Aktif, tamamlanan ve arşiv</small></span><ArrowRight /></Link>
        {firstProject ? <Link to={`/w/${wid}/projects/${firstProject.id}/tasks`}><Plus /><span><strong>Görevleri yönet</strong><small>Liste ve Kanban görünümü</small></span><ArrowRight /></Link> : null}
        {firstProject ? <Link className="quick-actions__ai" to={`/w/${wid}/projects/${firstProject.id}/copilot`}><Sparkles /><span><strong>Copilot’a sor</strong><small>Proje bağlamıyla kaynaklı yanıtlar</small></span><ArrowRight /></Link> : null}
      </section>
      <div className="dashboard-grid">
        <section className="content-card content-card--wide">
          <header className="section-header"><div><h2>Aktif projeler</h2><span>{projects.data?.items.filter((project) => project.status === 'Active').length ?? 0} proje</span></div><Link to={`/w/${wid}/projects`}>Tümünü gör</Link></header>
          <div className="project-cards">{projects.data?.items.filter((project) => project.status === 'Active').slice(0, 3).map((project) => (
            <Link to={`/w/${wid}/projects/${project.id}/overview`} className="project-card" key={project.id}>
              <span className={`project-card__accent project-accent--${project.id % 4}`} />
              <h3>{project.name}</h3><p>{project.description}</p><footer><span>AKTİF</span><ArrowRight /></footer>
            </Link>
          ))}</div>
        </section>
        <section className="content-card">
          <header className="section-header"><div><h2>Bana atananlar</h2><span>{openTasks.length} açık</span></div><ListIcon /></header>
          <div className="compact-task-list">{openTasks.slice(0, 5).map((task) => (
            <Link key={task.id} to={`/w/${wid}/projects/${task.projectId}/tasks/${task.id}`}>
              <StatusBadge status={task.status} compact /><span className="compact-task-list__title"><small>TP-{task.id}</small>{task.title}</span><PriorityBadge priority={task.priority} compact /><time className={formatRelativeDue(task).overdue ? 'is-overdue' : ''}>{formatRelativeDue(task).label}</time>
            </Link>
          ))}{!openTasks.length ? <p className="quiet-copy">Size atanmış açık görev yok.</p> : null}</div>
        </section>
        <section className="content-card">
          <header className="section-header"><div><h2>Yaklaşan tarihler</h2><span>Önümüzdeki 14 gün</span></div><CalendarClock /></header>
          <div className="deadline-list">{(tasks.data?.items ?? []).filter((task) => task.dueDate && !['Done', 'Cancelled'].includes(task.status)).slice(0, 4).map((task) => <Link key={task.id} to={`/w/${wid}/projects/${task.projectId}/tasks/${task.id}`}><time>{new Date(task.dueDate!).getDate()}<small>Ağu</small></time><span><strong>{task.title}</strong><small>TP-{task.id} · {task.priority}</small></span></Link>)}</div>
        </section>
        <section className="content-card ai-callout"><Bot /><div><span className="eyebrow">AURORA COPILOT</span><h2>Projenizin bağlamını kaybetmeden çalışın.</h2><p>Görevler ve notlar üzerinden kaynak gösteren yanıtlar alın.</p>{firstProject ? <Link className="button button--quiet" to={`/w/${wid}/projects/${firstProject.id}/copilot`}>Copilot’u aç</Link> : null}</div></section>
      </div>
    </div>
  );
}

function ListIcon() { return <span className="mono">⌘K</span>; }

function formatToday() {
  return new Intl.DateTimeFormat('tr-TR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })
    .format(new Date())
    .toLocaleUpperCase('tr-TR');
}
