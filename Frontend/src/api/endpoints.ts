import { apiRequest, apiSse } from './apiClient';
import { memberFromWire, pageFromWire, projectFromWire, projectMemberFromWire, taskFromWire, taskPriorityToWire, taskStatusToWire, type WireMember, type WireProject, type WireProjectMember, type WireTask } from './adapters';
import type {
  AuthResponse,
  CreateTaskRequest,
  KnowledgeGraph,
  ServiceResult,
  TaskFilters,
  UpdateNoteRequest,
} from './contracts';
import type {
  KnowledgeFolder,
  Note,
  Notification,
  PagedResponse,
  ProjectDashboard,
  TaskStatus,
  Workspace,
} from '../types/domain';

const query = (values: Record<string, string | number | undefined>) => {
  const params = new URLSearchParams();
  Object.entries(values).forEach(([key, value]) => value !== undefined && params.set(key, String(value)));
  const serialized = params.toString();
  return serialized ? `?${serialized}` : '';
};

export const authApi = {
  login: (email: string, password: string) =>
    apiRequest<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),
  register: (email: string, password: string) =>
    apiRequest<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) }),
  google: (idToken: string) =>
    apiRequest<AuthResponse>('/api/auth/google', { method: 'POST', body: JSON.stringify({ idToken }) }),
};

export const workspaceApi = {
  list: () => apiRequest<PagedResponse<Workspace>>('/api/workspaces?pageNumber=1&pageSize=100'),
  get: (workspaceId: number) => apiRequest<Workspace>(`/api/workspaces/${workspaceId}`),
  create: (name: string) => apiRequest<Workspace>('/api/workspaces', { method: 'POST', body: JSON.stringify({ name }) }),
  projects: (workspaceId: number) =>
    apiRequest<PagedResponse<WireProject>>(`/api/workspaces/${workspaceId}/projects?pageNumber=1&pageSize=100`).then((response) => pageFromWire(response, (project) => projectFromWire(project, workspaceId))),
  members: (workspaceId: number) => apiRequest<WireMember[]>(`/api/workspaces/${workspaceId}/members`).then((response) => response.map(memberFromWire)),
};

export const taskApi = {
  list: (projectId: number, filters: TaskFilters = {}) =>
    apiRequest<PagedResponse<WireTask>>(
      `/api/projects/${projectId}/tasks${query({ ...filters, pageSize: filters.pageSize ?? 100 })}`,
    ).then((response) => pageFromWire(response, taskFromWire)),
  get: (taskId: number) => apiRequest<WireTask>(`/api/tasks/${taskId}`).then(taskFromWire),
  create: (projectId: number, request: CreateTaskRequest) =>
    apiRequest<WireTask>(`/api/projects/${projectId}/tasks`, { method: 'POST', body: JSON.stringify({ ...request, priority: taskPriorityToWire(request.priority) }) }).then(taskFromWire),
  transition: (taskId: number, status: TaskStatus) =>
    apiRequest<WireTask>(`/api/tasks/${taskId}/status`, { method: 'PATCH', body: JSON.stringify({ status: taskStatusToWire(status) }) }).then(taskFromWire),
};

export const projectApi = {
  create: (workspaceId: number, name: string, description: string | null) =>
    apiRequest<WireProject>(`/api/workspaces/${workspaceId}/projects`, {
      method: 'POST',
      body: JSON.stringify({ name, description }),
    }).then((response) => projectFromWire(response, workspaceId)),
  members: (projectId: number) => apiRequest<WireProjectMember[]>(`/api/projects/${projectId}/members`).then((response) => response.map(projectMemberFromWire)),
};

export const dashboardApi = {
  get: (projectId: number) => apiRequest<ProjectDashboard>(`/api/projects/${projectId}/dashboard`),
};

export const knowledgeApi = {
  folders: (workspaceId: number, projectId?: number) =>
    apiRequest<KnowledgeFolder[]>(`/api/workspaces/${workspaceId}/folders${query({ projectId })}`),
  note: (noteId: number) => apiRequest<Note>(`/api/notes/${noteId}`),
  updateNote: (noteId: number, request: UpdateNoteRequest) =>
    apiRequest<Note>(`/api/notes/${noteId}`, {
      method: 'PUT',
      headers: { 'If-Match': String(request.expectedVersion) },
      body: JSON.stringify(request),
    }),
  graph: (workspaceId: number, projectId?: number) =>
    apiRequest<KnowledgeGraph>(`/api/workspaces/${workspaceId}/knowledge/graph${query({ projectId })}`),
};

export const notificationApi = {
  list: (unreadOnly = false) =>
    apiRequest<PagedResponse<Notification>>(`/api/notifications${query({ isRead: unreadOnly ? 'false' : undefined, pageSize: 100 })}`),
  markRead: (id: number) => apiRequest<ServiceResult<never>>(`/api/notifications/${id}/read`, { method: 'PATCH' }),
  markAllRead: () => apiRequest<ServiceResult<never>>('/api/notifications/read-all', { method: 'PATCH' }),
};

export const copilotApi = {
  streamProject: (projectId: number, message: string, sessionId: number | null, signal: AbortSignal, onEvent: (event: string, data: unknown) => void) =>
    apiSse(`/api/projects/${projectId}/ai/chat/stream`, { message, sessionId }, signal, onEvent),
};

export interface WeeklyReportResponse {
  id: number;
  projectId: number;
  status: 'PendingReview' | 'Approved' | 'Rejected';
  content: string;
  createdAtUtc: string;
  reviewedAtUtc: string | null;
}

export const reportsApi = {
  create: (projectId: number, weekEnding: string) => apiRequest<WireWeeklyReportResponse>(`/api/projects/${projectId}/ai/reports`, { method: 'POST', body: JSON.stringify({ sourceEventId: crypto.randomUUID(), weekEnding }) }).then(reportFromWire),
  get: (reportId: number) => apiRequest<WireWeeklyReportResponse>(`/api/ai/reports/${reportId}`).then(reportFromWire),
  review: (reportId: number, approve: boolean) => apiRequest<WireWeeklyReportResponse>(`/api/ai/reports/${reportId}/${approve ? 'approve' : 'reject'}`, { method: 'POST' }).then(reportFromWire),
};

export interface AiSuggestionResponse {
  id: number;
  projectId: number;
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Applied';
  title: string | null;
  priority: string | null;
  labels: string[];
  subtasks: string[];
  dueDate: string | null;
  appliedTaskId: number | null;
}

export const aiSuggestionApi = {
  create: (projectId: number, input: string) => apiRequest<WireAiSuggestionResponse>(`/api/projects/${projectId}/ai/task-suggestions`, { method: 'POST', body: JSON.stringify({ input }) }).then(aiSuggestionFromWire),
  get: (id: number) => apiRequest<WireAiSuggestionResponse>(`/api/ai/suggestions/${id}`).then(aiSuggestionFromWire),
  apply: (id: number) => apiRequest<WireAiSuggestionResponse>(`/api/ai/suggestions/${id}/apply`, { method: 'POST' }).then(aiSuggestionFromWire),
};

type WireWeeklyReportResponse = Omit<WeeklyReportResponse, 'status'> & { status: WeeklyReportResponse['status'] | number };
const weeklyStatuses: WeeklyReportResponse['status'][] = ['PendingReview', 'Approved', 'Rejected'];
const reportFromWire = (report: WireWeeklyReportResponse): WeeklyReportResponse => ({ ...report, status: typeof report.status === 'number' ? weeklyStatuses[report.status] ?? 'PendingReview' : report.status });

type WireAiSuggestionResponse = Omit<AiSuggestionResponse, 'status'> & { status: AiSuggestionResponse['status'] | number };
const aiStatuses: AiSuggestionResponse['status'][] = ['Pending', 'Processing', 'Completed', 'Failed', 'Applied'];
const aiSuggestionFromWire = (suggestion: WireAiSuggestionResponse): AiSuggestionResponse => ({ ...suggestion, status: typeof suggestion.status === 'number' ? aiStatuses[suggestion.status] ?? 'Failed' : suggestion.status });
