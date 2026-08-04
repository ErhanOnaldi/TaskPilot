import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { dashboardApi, knowledgeApi, notificationApi, projectApi, taskApi, workspaceApi } from './endpoints';
import { demoFolders, demoNotes, demoNotifications, demoProjects, demoTasks, demoWorkspace } from '../mocks/demoData';
import type { CreateTaskRequest, KnowledgeGraph } from './contracts';
import type { PagedResponse, ProjectDashboard, TaskItem, TaskStatus } from '../types/domain';

export const isDemoMode = import.meta.env.DEV && import.meta.env.VITE_DEMO_MODE !== 'false';

const page = <T,>(items: T[]): PagedResponse<T> => ({
  items,
  pageNumber: 1,
  pageSize: 100,
  totalCount: items.length,
  totalPages: items.length ? 1 : 0,
  hasPreviousPage: false,
  hasNextPage: false,
});

export const queryKeys = {
  workspaces: ['workspaces'] as const,
  workspace: (workspaceId: number) => ['workspace', workspaceId] as const,
  projects: (workspaceId: number) => ['projects', workspaceId] as const,
  tasks: (projectId: number) => ['tasks', projectId] as const,
  dashboard: (projectId: number) => ['dashboard', projectId] as const,
  folders: (workspaceId: number, projectId?: number) => ['folders', workspaceId, projectId] as const,
  note: (noteId: number) => ['note', noteId] as const,
  graph: (workspaceId: number, projectId?: number) => ['graph', workspaceId, projectId] as const,
  notifications: ['notifications'] as const,
  members: (workspaceId: number) => ['members', workspaceId] as const,
  projectMembers: (projectId: number) => ['project-members', projectId] as const,
};

export function useWorkspace(workspaceId: number) {
  return useQuery({
    queryKey: queryKeys.workspace(workspaceId),
    queryFn: () => (isDemoMode ? Promise.resolve(demoWorkspace) : workspaceApi.get(workspaceId)),
  });
}

export function useWorkspaces() {
  return useQuery({
    queryKey: queryKeys.workspaces,
    queryFn: () => (isDemoMode ? Promise.resolve(page([demoWorkspace])) : workspaceApi.list()),
  });
}

export function useCreateWorkspace() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (name: string) => workspaceApi.create(name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.workspaces }),
  });
}

export function useProjects(workspaceId: number) {
  return useQuery({
    queryKey: queryKeys.projects(workspaceId),
    queryFn: () => (isDemoMode ? Promise.resolve(page(demoProjects)) : workspaceApi.projects(workspaceId)),
  });
}

export function useCreateProject(workspaceId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ name, description }: { name: string; description: string | null }) =>
      projectApi.create(workspaceId, name, description),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: queryKeys.projects(workspaceId) }),
  });
}

export function useTasks(projectId: number) {
  return useQuery({
    queryKey: queryKeys.tasks(projectId),
    queryFn: () => (isDemoMode ? Promise.resolve(page(demoTasks.filter((task) => task.projectId === projectId))) : taskApi.list(projectId)),
  });
}

function dashboardFrom(tasks: TaskItem[], projectId: number): ProjectDashboard {
  const count = (status: TaskStatus) => tasks.filter((task) => task.status === status).length;
  const eligible = tasks.filter((task) => task.status !== 'Cancelled');
  const open = tasks.filter((task) => ['Todo', 'InProgress', 'InReview'].includes(task.status));
  return {
    projectId,
    totalTasks: tasks.length,
    todoTasks: count('Todo'),
    inProgressTasks: count('InProgress'),
    inReviewTasks: count('InReview'),
    doneTasks: count('Done'),
    cancelledTasks: count('Cancelled'),
    overdueTasks: open.filter((task) => task.dueDate && new Date(task.dueDate) < new Date('2026-08-02T12:00:00+03:00')).length,
    assignedTasks: tasks.filter((task) => task.assignedUserId).length,
    unassignedTasks: open.filter((task) => !task.assignedUserId).length,
    completionRate: eligible.length ? count('Done') / eligible.length : 0,
  };
}

export function useDashboard(projectId: number) {
  return useQuery({
    queryKey: queryKeys.dashboard(projectId),
    queryFn: () => isDemoMode ? Promise.resolve(dashboardFrom(demoTasks.filter((task) => task.projectId === projectId), projectId)) : dashboardApi.get(projectId),
  });
}

export function useTransitionTask(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ task, status }: { task: TaskItem; status: TaskStatus }) => {
      if (isDemoMode) return { ...task, status, updatedAt: new Date().toISOString() };
      return taskApi.transition(task.id, status);
    },
    onMutate: async ({ task, status }) => {
      const key = queryKeys.tasks(projectId);
      await queryClient.cancelQueries({ queryKey: key });
      const previous = queryClient.getQueryData<PagedResponse<TaskItem>>(key);
      queryClient.setQueryData<PagedResponse<TaskItem>>(key, (current) => current ? {
        ...current,
        items: current.items.map((item) => item.id === task.id ? { ...item, status } : item),
      } : current);
      return { previous };
    },
    onError: (_error, _variables, context) => {
      if (context?.previous) queryClient.setQueryData(queryKeys.tasks(projectId), context.previous);
    },
    onSettled: () => {
      void queryClient.invalidateQueries({ queryKey: queryKeys.tasks(projectId) });
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard(projectId) });
    },
  });
}

export function useCreateTask(projectId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateTaskRequest) => {
      if (!isDemoMode) return taskApi.create(projectId, request);
      const current = queryClient.getQueryData<PagedResponse<TaskItem>>(queryKeys.tasks(projectId));
      const id = Math.max(0, ...(current?.items.map((task) => task.id) ?? [])) + 1;
      const now = new Date().toISOString();
      return { id, projectId, ...request, status: 'Todo' as const, createdByUserId: 1, createdAt: now, updatedAt: now, completedAt: null };
    },
    onSuccess: (task) => {
      queryClient.setQueryData<PagedResponse<TaskItem>>(queryKeys.tasks(projectId), (current) => current ? {
        ...current,
        items: [task, ...current.items],
        totalCount: current.totalCount + 1,
      } : page([task]));
      void queryClient.invalidateQueries({ queryKey: queryKeys.dashboard(projectId) });
    },
  });
}

export function useFolders(workspaceId: number, projectId?: number) {
  return useQuery({
    queryKey: queryKeys.folders(workspaceId, projectId),
    queryFn: () => isDemoMode ? Promise.resolve(demoFolders.filter((folder) => !folder.projectId || folder.projectId === projectId)) : knowledgeApi.folders(workspaceId, projectId),
  });
}

export function useNote(noteId: number) {
  return useQuery({
    queryKey: queryKeys.note(noteId),
    enabled: noteId > 0,
    queryFn: () => isDemoMode ? Promise.resolve(demoNotes.find((note) => note.id === noteId) ?? demoNotes[0]!) : knowledgeApi.note(noteId),
  });
}

export function useUpdateNote(noteId: number) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: import('./contracts').UpdateNoteRequest) => {
      if (isDemoMode) return { ...(queryClient.getQueryData<import('../types/domain').Note>(queryKeys.note(noteId)) ?? demoNotes.find((note) => note.id === noteId)!), ...request, version: request.expectedVersion + 1, updatedAt: new Date().toISOString() };
      return knowledgeApi.updateNote(noteId, request);
    },
    onSuccess: (note) => queryClient.setQueryData(queryKeys.note(noteId), note),
  });
}

export function useGraph(workspaceId: number, projectId?: number) {
  return useQuery({
    queryKey: queryKeys.graph(workspaceId, projectId),
    queryFn: async (): Promise<KnowledgeGraph> => {
      if (!isDemoMode) return knowledgeApi.graph(workspaceId, projectId);
      return {
        nodes: demoNotes.map(({ id, title, slug }) => ({ noteId: id, title, slug })),
        edges: [
          { sourceNoteId: 1, targetNoteId: 3, targetSlug: 'password-recovery-flow' },
          { sourceNoteId: 1, targetNoteId: 4, targetSlug: 'api-security-decisions' },
          { sourceNoteId: 2, targetNoteId: 1, targetSlug: 'authentication-architecture' },
          { sourceNoteId: 3, targetNoteId: 2, targetSlug: 'incident-response-playbook' },
        ],
      };
    },
  });
}

export function useNotifications() {
  return useQuery({
    queryKey: queryKeys.notifications,
    queryFn: () => isDemoMode ? Promise.resolve(page(demoNotifications)) : notificationApi.list(),
  });
}

export function useMembers(workspaceId: number) {
  return useQuery({
    queryKey: queryKeys.members(workspaceId),
    queryFn: () => isDemoMode ? Promise.resolve(page([
      { userId: 1, email: 'elif@northstar.studio', role: 'Owner' as const, projectRole: 'ProjectManager' as const, joinedAt: '2026-01-12T09:00:00Z' },
      { userId: 2, email: 'jonas@northstar.studio', role: 'Manager' as const, projectRole: 'ProjectManager' as const, joinedAt: '2026-02-03T09:00:00Z' },
      { userId: 3, email: 'priya@northstar.studio', role: 'Member' as const, projectRole: 'TeamMember' as const, joinedAt: '2026-03-19T09:00:00Z' },
      { userId: 4, email: 'tomas@northstar.studio', role: 'Member' as const, projectRole: 'TeamMember' as const, joinedAt: '2026-04-02T09:00:00Z' },
      { userId: 6, email: 'sam@contractor.io', role: 'Guest' as const, projectRole: 'Guest' as const, joinedAt: '2026-07-14T09:00:00Z' },
    ])) : workspaceApi.members(workspaceId).then(page),
  });
}

export function useProjectMembers(projectId: number) {
  return useQuery({
    queryKey: queryKeys.projectMembers(projectId),
    enabled: projectId > 0,
    queryFn: () => isDemoMode ? Promise.resolve(page([
      { userId: 1, email: 'elif@northstar.studio', role: 'ProjectManager' as const, joinedAt: '2026-01-12T09:00:00Z' },
      { userId: 3, email: 'priya@northstar.studio', role: 'TeamMember' as const, joinedAt: '2026-03-19T09:00:00Z' },
      { userId: 6, email: 'sam@contractor.io', role: 'Guest' as const, joinedAt: '2026-07-14T09:00:00Z' },
    ])) : projectApi.members(projectId).then(page),
  });
}

export function useMarkAllNotificationsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      if (!isDemoMode) await notificationApi.markAllRead();
    },
    onSuccess: () => {
      queryClient.setQueryData<PagedResponse<import('../types/domain').Notification>>(queryKeys.notifications, (current) => current ? {
        ...current,
        items: current.items.map((item) => ({ ...item, isRead: true })),
      } : current);
    },
  });
}
