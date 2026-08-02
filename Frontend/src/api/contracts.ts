import type {
  Comment,
  KnowledgeFolder,
  Note,
  Notification,
  PagedResponse,
  Project,
  ProjectDashboard,
  TaskItem,
  TaskPriority,
  TaskStatus,
  Workspace,
} from '../types/domain';

export interface ServiceResult<T> {
  data?: T | null;
  errorMessages?: string[] | null;
}

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: { id: number; email: string };
}

export interface CreateTaskRequest {
  title: string;
  description: string | null;
  dueDate: string | null;
  priority: TaskPriority;
  assignedUserId: number | null;
}

export interface UpdateNoteRequest {
  title: string;
  slug: string;
  content: string;
  projectId: number | null;
  folderId: number | null;
  expectedVersion: number;
}

export interface NoteLink {
  sourceNoteId: number;
  targetNoteId: number | null;
  targetSlug: string;
  alias: string | null;
  isResolved: boolean;
}

export interface KnowledgeGraph {
  nodes: Array<{ noteId: number; title: string; slug: string }>;
  edges: Array<{ sourceNoteId: number; targetNoteId: number | null; targetSlug: string }>;
}

export type ApiContracts = {
  workspaces: PagedResponse<Workspace>;
  projects: PagedResponse<Project>;
  tasks: PagedResponse<TaskItem>;
  task: TaskItem;
  comments: PagedResponse<Comment>;
  folders: KnowledgeFolder[];
  note: Note;
  notifications: PagedResponse<Notification>;
  dashboard: ProjectDashboard;
};

export interface TaskFilters {
  search?: string;
  status?: TaskStatus;
  priority?: TaskPriority;
  assignedUserId?: number;
  pageNumber?: number;
  pageSize?: number;
}
