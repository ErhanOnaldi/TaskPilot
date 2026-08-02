export type TaskStatus = 'Todo' | 'InProgress' | 'InReview' | 'Done' | 'Cancelled';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';
export type ProjectStatus = 'Active' | 'Completed' | 'Archived';
export type WorkspaceRole = 'Owner' | 'Manager' | 'Member' | 'Guest';
export type ProjectRole = 'ProjectManager' | 'TeamMember' | 'Guest';

export interface Workspace {
  id: number;
  name: string;
  isArchived: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Project {
  id: number;
  workspaceId: number;
  name: string;
  description: string | null;
  status: ProjectStatus;
  createdAt: string;
  updatedAt: string;
}

export interface TaskItem {
  id: number;
  projectId: number;
  title: string;
  description: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  dueDate: string | null;
  assignedUserId: number | null;
  createdByUserId: number;
  createdAt: string;
  updatedAt: string;
  completedAt: string | null;
}

export interface Member {
  userId: number;
  email: string;
  role: WorkspaceRole;
  projectRole?: ProjectRole;
  joinedAt: string;
}

export interface ProjectMembership {
  userId: number;
  email: string;
  role: ProjectRole;
  joinedAt: string;
}

export interface Comment {
  id: number;
  taskId: number;
  userId: number;
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface Note {
  id: number;
  workspaceId: number;
  projectId: number | null;
  folderId: number | null;
  title: string;
  slug: string;
  content: string;
  version: number;
  updatedAt: string;
}

export interface KnowledgeFolder {
  id: number;
  workspaceId: number;
  projectId: number | null;
  parentFolderId: number | null;
  name: string;
  slug: string;
  updatedAt: string;
}

export interface Notification {
  id: number;
  type: string;
  title: string;
  message: string;
  isRead: boolean;
  relatedEntityId: number | null;
  createdAt: string;
}

export interface ProjectDashboard {
  projectId: number;
  totalTasks: number;
  todoTasks: number;
  inProgressTasks: number;
  inReviewTasks: number;
  doneTasks: number;
  cancelledTasks: number;
  overdueTasks: number;
  assignedTasks: number;
  unassignedTasks: number;
  completionRate: number;
}

export interface PagedResponse<T> {
  items: T[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UserContext {
  id: number;
  email: string;
  workspaceRole: WorkspaceRole;
  projectRole: ProjectRole;
}
