import { Bell, Bot, FolderKanban, Home, ListTodo } from 'lucide-react';
import { useEffect, useState, useSyncExternalStore } from 'react';
import { Navigate, NavLink, Outlet, useParams } from 'react-router-dom';
import { CommandPalette } from '../../components/CommandPalette';
import { NavigationSidebar } from './NavigationSidebar';
import { TopToolbar } from './TopToolbar';
import { useAppContext } from '../../app/AppProviders';
import { isDemoMode, useMembers, useProjectMembers, useProjects } from '../../api/dataSource';
import { hasSession, subscribeSession } from '../../api/apiClient';

export function AppShell() {
  const authenticated = useSyncExternalStore(subscribeSession, hasSession, () => false);
  return !isDemoMode && !authenticated ? <Navigate replace to="/login" /> : <AppShellContent />;
}

function AppShellContent() {
  const [sidebarOpen, setSidebarOpen] = useState(false);
  const [paletteOpen, setPaletteOpen] = useState(false);
  const { workspaceId = '1', projectId } = useParams();
  const { user, setUserRoles } = useAppContext();
  const members = useMembers(Number(workspaceId));
  // Without a project in the route there is nothing to look up; querying id 1 would 404 for most users.
  const projectMembers = useProjectMembers(Number(projectId ?? 0));
  const projects = useProjects(Number(workspaceId));
  const activeProject = projects.data?.items.find((project) => String(project.id) === projectId)
    ?? projects.data?.items.find((project) => project.status === 'Active')
    ?? projects.data?.items[0];
  useEffect(() => {
    if (!user.id) return;
    const workspaceRole = members.data?.items.find((member) => member.userId === user.id)?.role ?? 'Guest';
    const projectRole = projectMembers.data?.items.find((member) => member.userId === user.id)?.role ?? 'Guest';
    if (workspaceRole !== user.workspaceRole || projectRole !== user.projectRole) setUserRoles(workspaceRole, projectRole);
  }, [members.data?.items, projectMembers.data?.items, setUserRoles, user.id, user.projectRole, user.workspaceRole]);
  useEffect(() => {
    const onKey = (event: KeyboardEvent) => {
      if ((event.metaKey || event.ctrlKey) && event.key.toLowerCase() === 'k') {
        event.preventDefault();
        setPaletteOpen((open) => !open);
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, []);

  return (
    <div className="app-shell">
      <NavigationSidebar open={sidebarOpen} onClose={() => setSidebarOpen(false)} />
      {sidebarOpen ? <button className="sidebar-scrim" onClick={() => setSidebarOpen(false)} aria-label="Menüyü kapat" /> : null}
      <div className="app-surface">
        <TopToolbar onMenu={() => setSidebarOpen(true)} onPalette={() => setPaletteOpen(true)} />
        <main className="main-workspace" id="main-content"><Outlet /></main>
      </div>
      <nav className="bottom-nav" aria-label="Mobil navigasyon">
        <NavLink end to={`/w/${workspaceId}`}><Home /><span>Ana Sayfa</span></NavLink>
        <NavLink to={activeProject ? `/w/${workspaceId}/projects/${activeProject.id}/tasks` : `/w/${workspaceId}/my-tasks`}><ListTodo /><span>Görevler</span></NavLink>
        <NavLink to={`/w/${workspaceId}/projects`}><FolderKanban /><span>Projeler</span></NavLink>
        {activeProject ? <NavLink to={`/w/${workspaceId}/projects/${activeProject.id}/copilot`}><Bot /><span>AI</span></NavLink> : null}
        <NavLink to={`/w/${workspaceId}/notifications`}><Bell /><span>Bildirimler</span></NavLink>
      </nav>
      <CommandPalette open={paletteOpen} onClose={() => setPaletteOpen(false)} />
    </div>
  );
}
