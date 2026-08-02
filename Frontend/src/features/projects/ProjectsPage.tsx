import { ArrowRight, Plus, Search } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useProjects } from '../../api/dataSource';
import { PageHeader } from '../../components/PageHeader';
import { Button } from '../../components/ui/Button';
import { EmptyState, LoadingSkeleton, ProblemState } from '../../components/ui/States';
import type { ProjectStatus } from '../../types/domain';
import { CreateProjectDialog } from './CreateProjectDialog';
import { useAppContext } from '../../app/AppProviders';

const filters: Array<{ value: ProjectStatus; label: string }> = [{ value: 'Active', label: 'Aktif' }, { value: 'Completed', label: 'Tamamlanan' }, { value: 'Archived', label: 'Arşiv' }];

export function ProjectsPage() {
  const { workspaceId = '1' } = useParams();
  const projects = useProjects(Number(workspaceId));
  const [filter, setFilter] = useState<ProjectStatus>('Active');
  const [search, setSearch] = useState('');
  const [createOpen, setCreateOpen] = useState(false);
  const { user } = useAppContext();
  const rows = useMemo(() => (projects.data?.items ?? []).filter((project) => project.status === filter && project.name.toLocaleLowerCase('tr').includes(search.toLocaleLowerCase('tr'))), [filter, projects.data?.items, search]);
  if (projects.isLoading) return <LoadingSkeleton rows={8} />;
  if (projects.error) return <ProblemState error={projects.error} onRetry={() => void projects.refetch()} />;
  return (
    <div className="page">
      <PageHeader title="Projeler" description="Aktif çalışmaları, tamamlananları ve arşivlenmiş projeleri yönetin." actions={['Owner', 'Manager'].includes(user.workspaceRole) ? <Button variant="primary" icon={<Plus />} onClick={() => setCreateOpen(true)}>Yeni proje</Button> : null} />
      <div className="filter-bar"><div className="segmented-control">{filters.map((item) => <button className={filter === item.value ? 'is-active' : ''} key={item.value} onClick={() => setFilter(item.value)}>{item.label}</button>)}</div><label className="search-field"><Search /><span className="sr-only">Proje ara</span><input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Proje ara…" /></label><span className="result-count">{rows.length} proje</span></div>
      {rows.length ? <div className="project-list">{rows.map((project) => <Link className="project-list__row" key={project.id} to={`/w/${workspaceId}/projects/${project.id}/overview`}><span className={`project-card__accent project-accent--${project.id % 4}`} /><div><h2>{project.name}</h2><p>{project.description}</p></div><span className={`state-text state-text--${project.status.toLowerCase()}`}>{project.status === 'Active' ? '◐ Aktif' : project.status === 'Completed' ? '● Tamamlandı' : '⊘ Arşiv'}</span><time>{new Intl.DateTimeFormat('tr-TR', { day: 'numeric', month: 'short' }).format(new Date(project.updatedAt))}</time><ArrowRight /></Link>)}</div> : <EmptyState title="Bu görünümde proje yok" description="Arama terimini veya durum filtresini değiştirin." action={{ label: 'Filtreleri temizle', onClick: () => { setFilter('Active'); setSearch(''); } }} />}
      <CreateProjectDialog open={createOpen} onClose={() => setCreateOpen(false)} workspaceId={Number(workspaceId)} />
    </div>
  );
}
