import type { ProjectRole, TaskItem, TaskStatus, UserContext, WorkspaceRole } from '../types/domain';

export const taskTransitions: Record<TaskStatus, TaskStatus[]> = {
  Todo: ['InProgress', 'Cancelled'],
  InProgress: ['InReview', 'Cancelled'],
  InReview: ['Done', 'Cancelled'],
  Done: ['InProgress'],
  Cancelled: ['InProgress'],
};

export function canTransitionTask(
  task: TaskItem,
  target: TaskStatus,
  user: UserContext,
  projectArchived: boolean,
): { allowed: boolean; reason?: string } {
  if (projectArchived) return { allowed: false, reason: 'Arşivlenmiş projede değişiklik yapılamaz.' };
  if (user.projectRole === 'Guest' || user.workspaceRole === 'Guest') {
    return { allowed: false, reason: 'Guest görev durumunu değiştiremez.' };
  }
  if (!taskTransitions[task.status].includes(target)) {
    return { allowed: false, reason: 'Bu durum geçişi görev akışında tanımlı değil.' };
  }
  if (user.projectRole === 'TeamMember' && task.assignedUserId !== user.id) {
    return { allowed: false, reason: 'Team Member yalnız kendisine atanmış görevi taşıyabilir.' };
  }
  if (task.status === 'Cancelled' && !['Owner', 'Manager'].includes(user.workspaceRole)) {
    return { allowed: false, reason: 'İptal edilen görevi yalnız Manager veya Owner yeniden açabilir.' };
  }
  return { allowed: true };
}

export function canCreateTask(role: ProjectRole, projectArchived: boolean) {
  return role !== 'Guest' && !projectArchived;
}

export function canEditKnowledge(role: WorkspaceRole, projectArchived: boolean) {
  return role !== 'Guest' && !projectArchived;
}
