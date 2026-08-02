import { Bell, Bot, ChevronDown, CircleUserRound, FolderKanban, GitFork, Home, ListTodo, Settings, UsersRound, X } from 'lucide-react';
import { NavLink, useParams } from 'react-router-dom';
import { useNotifications, useProjects, useWorkspace } from '../../api/dataSource';
import { MemberAvatar } from '../../components/ui/Avatar';
import { useAppContext } from '../../app/AppProviders';

const navClass = ({ isActive }: { isActive: boolean }) => `nav-link${isActive ? ' nav-link--active' : ''}`;

export function NavigationSidebar({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { workspaceId = '1', projectId } = useParams();
  const wid = Number(workspaceId);
  const { user } = useAppContext();
  const workspace = useWorkspace(wid).data;
  const projects = useProjects(wid).data?.items ?? [];
  const unread = useNotifications().data?.items.filter((item) => !item.isRead).length ?? 0;

  return (
    <aside className={`navigation-sidebar${open ? ' navigation-sidebar--open' : ''}`} aria-label="Ana navigasyon">
      <header className="sidebar-workspace">
        <div className="workspace-avatar">{workspace?.name.slice(0, 2).toUpperCase() ?? 'TP'}</div>
        <div><strong>{workspace?.name ?? 'Workspace'}</strong><span>{user.workspaceRole}</span></div>
        <ChevronDown aria-hidden="true" />
        <button className="icon-button sidebar-close" onClick={onClose} aria-label="Menüyü kapat"><X /></button>
      </header>
      <nav className="sidebar-nav">
        <NavLink end className={navClass} to={`/w/${workspaceId}`} onClick={onClose}><Home />Genel Bakış</NavLink>
        <NavLink className={navClass} to={`/w/${workspaceId}/projects`} onClick={onClose}><FolderKanban />Projeler</NavLink>
        <NavLink className={navClass} to={`/w/${workspaceId}/my-tasks`} onClick={onClose}><ListTodo />Görevlerim</NavLink>
        <NavLink className={navClass} to={`/w/${workspaceId}/notifications`} onClick={onClose}><Bell />Bildirimler{unread ? <span className="nav-count">{unread}</span> : null}</NavLink>
      </nav>
      <section className="project-tree" aria-labelledby="project-tree-title">
        <div className="sidebar-section-title" id="project-tree-title">Projeler</div>
        {projects.filter((project) => project.status !== 'Completed').map((project) => (
          <div className="project-tree__item" key={project.id}>
            <NavLink className={navClass} to={`/w/${workspaceId}/projects/${project.id}/overview`} onClick={onClose}>
              <span className={`project-accent project-accent--${project.id % 4}`} />{project.name}
            </NavLink>
            {String(project.id) === projectId ? (
              <div className="project-tree__children">
                <NavLink className={navClass} to={`/w/${workspaceId}/projects/${project.id}/tasks`} onClick={onClose}><ListTodo />Görevler</NavLink>
                <NavLink className={navClass} to={`/w/${workspaceId}/projects/${project.id}/knowledge`} onClick={onClose}><FolderKanban />Bilgi Merkezi</NavLink>
                <NavLink className={navClass} to={`/w/${workspaceId}/projects/${project.id}/graph`} onClick={onClose}><GitFork />Bilgi Grafiği</NavLink>
                <NavLink className={navClass} to={`/w/${workspaceId}/projects/${project.id}/copilot`} onClick={onClose}><Bot />AI Copilot</NavLink>
              </div>
            ) : null}
          </div>
        ))}
      </section>
      <nav className="sidebar-nav sidebar-nav--bottom">
        {user.workspaceRole !== 'Guest' ? <NavLink className={navClass} to={`/w/${workspaceId}/members`} onClick={onClose}><UsersRound />Üyeler</NavLink> : null}
        {['Owner', 'Manager'].includes(user.workspaceRole) ? <NavLink className={navClass} to={`/w/${workspaceId}/settings`} onClick={onClose}><Settings />Ayarlar</NavLink> : null}
      </nav>
      <footer className="sidebar-profile"><MemberAvatar name={user.email || 'Kullanıcı'} /><div><strong>{user.email.split('@')[0] || 'Kullanıcı'}</strong><span>{user.projectRole}</span></div><CircleUserRound /></footer>
    </aside>
  );
}
