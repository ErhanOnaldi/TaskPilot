import { Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useProjects } from '../api/dataSource';
import { Dialog } from './ui/Dialog';

export function CommandPalette({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [query, setQuery] = useState('');
  const navigate = useNavigate();
  const { workspaceId = '1', projectId } = useParams();
  const projectPage = useProjects(Number(workspaceId)).data;
  useEffect(() => { if (!open) setQuery(''); }, [open]);
  const commands = useMemo(() => {
    const projects = projectPage?.items ?? [];
    // Project-scoped commands follow the route, then fall back to a real project instead of a guessed id.
    const project = projects.find((item) => String(item.id) === projectId)
      ?? projects.find((item) => item.status === 'Active')
      ?? projects[0];
    const base = [
      { label: 'Workspace ana sayfasına git', path: `/w/${workspaceId}` },
      { label: 'Projeleri aç', path: `/w/${workspaceId}/projects` },
      { label: 'Görevlerim', path: `/w/${workspaceId}/my-tasks` },
      { label: 'Bildirimleri aç', path: `/w/${workspaceId}/notifications` },
    ];
    const scoped = project ? [
      { label: `Görevleri aç · ${project.name}`, path: `/w/${workspaceId}/projects/${project.id}/tasks` },
      { label: `Kanban görünümünü aç · ${project.name}`, path: `/w/${workspaceId}/projects/${project.id}/tasks?view=kanban` },
      { label: `Bilgi Merkezi’ni aç · ${project.name}`, path: `/w/${workspaceId}/projects/${project.id}/knowledge` },
      { label: `Copilot’u aç · ${project.name}`, path: `/w/${workspaceId}/projects/${project.id}/copilot` },
    ] : [];
    const projectLinks = projects.map((item) => ({ label: `Proje: ${item.name}`, path: `/w/${workspaceId}/projects/${item.id}/overview` }));
    return [...base, ...scoped, ...projectLinks]
      .filter((command) => command.label.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr')));
  }, [projectId, projectPage, query, workspaceId]);
  return (
    <Dialog open={open} onClose={onClose} title="Komut paleti" description="Sayfaya git veya bir komut çalıştır." size="md">
      <label className="search-field"><Search aria-hidden="true" /><span className="sr-only">Komut ara</span><input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Görev, not veya komut ara…" /></label>
      <div className="command-list">
        {commands.map((command) => <button key={command.path} onClick={() => { navigate(command.path); onClose(); }}><span>{command.label}</span><kbd>↵</kbd></button>)}
        {commands.length ? null : <p className="quiet-copy">Eşleşen komut yok.</p>}
      </div>
    </Dialog>
  );
}
