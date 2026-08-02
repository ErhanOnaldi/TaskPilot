import { AlertTriangle, CheckCircle2, Circle, CircleDashed, Gauge, Users } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { useDashboard, useProjects, useTasks } from '../../api/dataSource';
import { MetricCard } from '../../components/MetricCard';
import { PageHeader } from '../../components/PageHeader';
import { PriorityBadge, StatusBadge } from '../../components/ui/Badge';
import { LoadingSkeleton, ProblemState } from '../../components/ui/States';
import { formatRelativeDue } from '../../lib/formatters';

export function ProjectOverviewPage() {
  const { workspaceId = '1', projectId = '1' } = useParams();
  const pid = Number(projectId);
  const project = useProjects(Number(workspaceId)).data?.items.find((item) => item.id === pid);
  const dashboard = useDashboard(pid);
  const tasks = useTasks(pid);
  if (dashboard.isLoading || tasks.isLoading) return <LoadingSkeleton rows={8} />;
  if (dashboard.error || tasks.error) return <ProblemState error={dashboard.error ?? tasks.error} onRetry={() => { void dashboard.refetch(); void tasks.refetch(); }} />;
  const data = dashboard.data!;
  const risky = (tasks.data?.items ?? []).filter((task) => ['Critical', 'High'].includes(task.priority) && !['Done', 'Cancelled'].includes(task.status));
  return (
    <div className="page">
      {project?.status === 'Archived' ? <div className="archive-banner">Bu proje arşivlenmiştir. İçerik salt okunur.</div> : null}
      <PageHeader eyebrow={project?.status === 'Active' ? '● AKTİF PROJE' : project?.status} title={project?.name ?? 'Proje dashboard'} description={project?.description ?? undefined} actions={<Link className="button button--primary" to={`/w/${workspaceId}/projects/${pid}/tasks`}>Görevleri aç</Link>} />
      <section className="metric-grid">
        <MetricCard label="Toplam görev" value={String(data.totalTasks)} icon={<Gauge />} />
        <MetricCard label="Todo" value={String(data.todoTasks)} icon={<Circle />} />
        <MetricCard label="Devam ediyor" value={String(data.inProgressTasks)} icon={<CircleDashed />} tone="info" />
        <MetricCard label="İncelemede" value={String(data.inReviewTasks)} icon={<AlertTriangle />} tone="warning" />
        <MetricCard label="Tamamlandı" value={String(data.doneTasks)} icon={<CheckCircle2 />} tone="success" />
        <MetricCard label="Gecikmiş" value={String(data.overdueTasks)} icon={<AlertTriangle />} tone={data.overdueTasks ? 'danger' : 'default'} hint="Yalnız açık görevler" />
        <MetricCard label="Atanmamış" value={String(data.unassignedTasks)} icon={<Users />} />
        <MetricCard label="Completion rate" value={`${Math.round(data.completionRate * 100)}%`} icon={<Gauge />} tone="success" hint="Cancelled görevler hesaba dahil edilmez" />
      </section>
      <div className="overview-grid">
        <section className="content-card content-card--wide"><header className="section-header"><div><h2>Riskteki görevler</h2><span>{risky.length} açık</span></div></header><div className="risk-list">{risky.map((task) => <Link key={task.id} to={`/w/${workspaceId}/projects/${pid}/tasks/${task.id}`}><span className="mono">TP-{task.id}</span><strong>{task.title}</strong><StatusBadge status={task.status} /><PriorityBadge priority={task.priority} /><time className={formatRelativeDue(task).overdue ? 'is-overdue' : ''}>{formatRelativeDue(task).label}</time></Link>)}</div></section>
        <section className="content-card"><header className="section-header"><div><h2>Durum dağılımı</h2><span>{data.totalTasks} görev</span></div></header>{(['Todo', 'InProgress', 'InReview', 'Done', 'Cancelled'] as const).map((status) => { const count = tasks.data?.items.filter((task) => task.status === status).length ?? 0; return <div className="distribution-row" key={status}><StatusBadge status={status} /><div><span style={{ width: `${data.totalTasks ? (count / data.totalTasks) * 100 : 0}%` }} /></div><strong>{count}</strong></div>; })}</section>
      </div>
    </div>
  );
}
