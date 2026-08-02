import { Search } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { Dialog } from './ui/Dialog';

export function CommandPalette({ open, onClose }: { open: boolean; onClose: () => void }) {
  const [query, setQuery] = useState('');
  const navigate = useNavigate();
  const { workspaceId = '1', projectId = '1' } = useParams();
  useEffect(() => { if (!open) setQuery(''); }, [open]);
  const commands = useMemo(() => [
    { label: 'Workspace ana sayfasına git', path: `/w/${workspaceId}` },
    { label: 'Görevleri aç', path: `/w/${workspaceId}/projects/${projectId}/tasks` },
    { label: 'Kanban görünümünü aç', path: `/w/${workspaceId}/projects/${projectId}/tasks?view=kanban` },
    { label: 'Bilgi Merkezi’ni aç', path: `/w/${workspaceId}/projects/${projectId}/knowledge` },
    { label: 'Copilot’u aç', path: `/w/${workspaceId}/projects/${projectId}/copilot` },
  ].filter((command) => command.label.toLocaleLowerCase('tr').includes(query.toLocaleLowerCase('tr'))), [projectId, query, workspaceId]);
  return (
    <Dialog open={open} onClose={onClose} title="Komut paleti" description="Sayfaya git veya bir komut çalıştır." size="md">
      <label className="search-field"><Search aria-hidden="true" /><span className="sr-only">Komut ara</span><input autoFocus value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Görev, not veya komut ara…" /></label>
      <div className="command-list">{commands.map((command) => <button key={command.path} onClick={() => { navigate(command.path); onClose(); }}><span>{command.label}</span><kbd>↵</kbd></button>)}</div>
    </Dialog>
  );
}
