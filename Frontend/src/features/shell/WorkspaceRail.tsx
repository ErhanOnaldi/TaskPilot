import { Plus, Settings } from 'lucide-react';
import { Link, useParams } from 'react-router-dom';
import { useWorkspace } from '../../api/dataSource';

export function WorkspaceRail() {
  const { workspaceId = '1' } = useParams();
  const workspaceName = useWorkspace(Number(workspaceId)).data?.name ?? 'Workspace';
  const initials = workspaceName
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toLocaleUpperCase('tr-TR'))
    .join('') || 'W';
  return (
    <aside className="workspace-rail" aria-label="Workspace seçimi">
      <Link className="brand-mark" to={`/w/${workspaceId}`} aria-label="TaskPilot ana sayfa">T</Link>
      <nav>
        <Link className="workspace-dot workspace-dot--active" to={`/w/${workspaceId}`} aria-label={workspaceName}>{initials}</Link>
        <button className="workspace-dot" aria-label="Workspace ekle"><Plus /></button>
      </nav>
      <Link className="icon-button" to={`/w/${workspaceId}/settings`} aria-label="Ayarlar"><Settings /></Link>
    </aside>
  );
}
