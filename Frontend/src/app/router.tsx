import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppShell } from '../features/shell/AppShell';
import { AuthPage } from '../features/auth/AuthPage';
import { WorkspaceHomePage } from '../features/dashboard/WorkspaceHomePage';
import { ProjectsPage } from '../features/projects/ProjectsPage';
import { ProjectOverviewPage } from '../features/dashboard/ProjectOverviewPage';
import { TasksPage } from '../features/tasks/TasksPage';
import { KnowledgePage } from '../features/knowledge/KnowledgePage';
import { KnowledgeGraphPage } from '../features/knowledge/KnowledgeGraphPage';
import { CopilotPage } from '../features/copilot/CopilotPage';
import { ReportsPage } from '../features/reports/ReportsPage';
import { NotificationCenter } from '../features/notifications/NotificationCenter';
import { MembersPage } from '../features/people/MembersPage';
import { SettingsPage } from '../features/settings/SettingsPage';
import { MyTasksPage } from '../features/tasks/MyTasksPage';
import { isDemoMode } from '../api/dataSource';
import { OnboardingPage } from '../features/onboarding/OnboardingPage';
import { LandingPage } from '../features/landing/LandingPage';

export const router = createBrowserRouter([
  { path: '/', element: <LandingPage /> },
  { path: '/login', element: <AuthPage /> },
  { path: '/register', element: <AuthPage /> },
  { path: '/onboarding', element: <OnboardingPage /> },
  {
    path: '/w/:workspaceId',
    element: <AppShell />,
    children: [
      { index: true, element: <WorkspaceHomePage /> },
      { path: 'projects', element: <ProjectsPage /> },
      { path: 'my-tasks', element: <MyTasksPage /> },
      { path: 'projects/:projectId/overview', element: <ProjectOverviewPage /> },
      { path: 'projects/:projectId/tasks', element: <TasksPage /> },
      { path: 'projects/:projectId/tasks/:taskId', element: <TasksPage /> },
      { path: 'projects/:projectId/knowledge', element: <KnowledgePage /> },
      { path: 'projects/:projectId/knowledge/:noteId', element: <KnowledgePage /> },
      { path: 'projects/:projectId/graph', element: <KnowledgeGraphPage /> },
      { path: 'projects/:projectId/copilot', element: <CopilotPage /> },
      { path: 'projects/:projectId/reports', element: <ReportsPage /> },
      { path: 'notifications', element: <NotificationCenter /> },
      { path: 'members', element: <MembersPage /> },
      { path: 'settings', element: <SettingsPage /> },
      { path: 'projects/:projectId/settings', element: <SettingsPage /> },
    ],
  },
  { path: '*', element: <Navigate replace to={isDemoMode ? '/w/1' : '/login'} /> },
]);
