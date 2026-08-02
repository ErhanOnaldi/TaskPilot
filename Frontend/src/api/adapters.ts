import type { Member, PagedResponse, Project, ProjectMembership, ProjectRole, ProjectStatus, TaskItem, TaskPriority, TaskStatus, WorkspaceRole } from '../types/domain';

const taskStatuses: TaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Done', 'Cancelled'];
const taskPriorities: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical'];
const projectStatuses: ProjectStatus[] = ['Active', 'Completed', 'Archived'];
const workspaceRoles: WorkspaceRole[] = ['Owner', 'Manager', 'Member', 'Guest'];
const projectRoles: ProjectRole[] = ['ProjectManager', 'TeamMember', 'Guest'];

function enumValue<T extends string>(value: T | number, values: T[], fallback: T): T {
  if (typeof value === 'number') return values[value] ?? fallback;
  return values.includes(value) ? value : fallback;
}

export const taskStatusFromWire = (value: TaskStatus | number) => enumValue(value, taskStatuses, 'Todo');
export const taskPriorityFromWire = (value: TaskPriority | number) => enumValue(value, taskPriorities, 'Medium');
export const projectStatusFromWire = (value: ProjectStatus | number) => enumValue(value, projectStatuses, 'Active');
export const workspaceRoleFromWire = (value: WorkspaceRole | number) => enumValue(value, workspaceRoles, 'Guest');
export const projectRoleFromWire = (value: ProjectRole | number) => enumValue(value, projectRoles, 'Guest');
export const taskStatusToWire = (value: TaskStatus) => taskStatuses.indexOf(value);
export const taskPriorityToWire = (value: TaskPriority) => taskPriorities.indexOf(value);

export type WireTask = Omit<TaskItem, 'status' | 'priority'> & { status: TaskStatus | number; priority: TaskPriority | number };
export type WireProject = Omit<Project, 'workspaceId' | 'status'> & { workspaceId?: number; status: ProjectStatus | number };
export type WireMember = Omit<Member, 'role' | 'projectRole'> & { role: WorkspaceRole | number; projectRole?: ProjectRole | number };
export type WireProjectMember = Omit<ProjectMembership, 'role'> & { role: ProjectRole | number };

export const taskFromWire = (task: WireTask): TaskItem => ({ ...task, status: taskStatusFromWire(task.status), priority: taskPriorityFromWire(task.priority) });
export const projectFromWire = (project: WireProject, workspaceId: number): Project => ({ ...project, workspaceId: project.workspaceId ?? workspaceId, status: projectStatusFromWire(project.status) });
export const memberFromWire = (member: WireMember): Member => {
  const { role, projectRole, ...rest } = member;
  return { ...rest, role: workspaceRoleFromWire(role), ...(projectRole === undefined ? {} : { projectRole: projectRoleFromWire(projectRole) }) };
};
export const projectMemberFromWire = (member: WireProjectMember): ProjectMembership => ({ ...member, role: projectRoleFromWire(member.role) });

export function pageFromWire<TWire, TDomain>(response: PagedResponse<TWire>, map: (item: TWire) => TDomain): PagedResponse<TDomain> {
  return { ...response, items: response.items.map(map) };
}
