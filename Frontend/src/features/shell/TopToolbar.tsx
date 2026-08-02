import { Bell, Menu, Moon, Search, Sun } from 'lucide-react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { useAppContext } from '../../app/AppProviders';
import { useNotifications, useProjects, useWorkspace } from '../../api/dataSource';

const labels: Record<string, string> = {
  projects: 'Projeler', overview: 'Dashboard', tasks: 'Görevler', knowledge: 'Bilgi Merkezi', graph: 'Bilgi Grafiği',
  copilot: 'AI Copilot', reports: 'Haftalık Raporlar', notifications: 'Bildirimler', members: 'Üyeler', settings: 'Ayarlar', 'my-tasks': 'Görevlerim',
};

export function TopToolbar({ onMenu, onPalette }: { onMenu: () => void; onPalette: () => void }) {
  const { workspaceId = '1', projectId } = useParams();
  const { pathname } = useLocation();
  const { theme, toggleTheme } = useAppContext();
  const workspace = useWorkspace(Number(workspaceId)).data;
  const project = useProjects(Number(workspaceId)).data?.items.find((item) => item.id === Number(projectId));
  const unread = useNotifications().data?.items.filter((item) => !item.isRead).length ?? 0;
  const segment = pathname.split('/').filter(Boolean).at(-1) ?? '';
  const title = labels[segment] ?? (pathname.endsWith(`/w/${workspaceId}`) ? 'Genel Bakış' : 'TaskPilot');
  return (
    <header className="top-toolbar">
      <button className="icon-button toolbar-menu" onClick={onMenu} aria-label="Navigasyonu aç"><Menu /></button>
      <nav className="breadcrumbs" aria-label="İçerik yolu">
        <Link to={`/w/${workspaceId}`}>{workspace?.name ?? 'Workspace'}</Link>
        {project ? <><span>/</span><Link to={`/w/${workspaceId}/projects/${project.id}/overview`}>{project.name}</Link></> : null}
        <span>/</span><strong>{title}</strong>
      </nav>
      <button className="command-trigger" onClick={onPalette}><Search /><span>Ara veya komut çalıştır…</span><kbd>⌘K</kbd></button>
      <button className="icon-button" onClick={toggleTheme} aria-label={theme === 'dark' ? 'Açık temaya geç' : 'Koyu temaya geç'}>{theme === 'dark' ? <Sun /> : <Moon />}</button>
      <Link className="icon-button icon-button--badge" to={`/w/${workspaceId}/notifications`} aria-label={`${unread} okunmamış bildirim`}><Bell />{unread ? <span>{unread}</span> : null}</Link>
    </header>
  );
}
