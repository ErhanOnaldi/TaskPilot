import { describe, expect, it } from 'vitest';
import { canTransitionTask, taskTransitions } from './taskRules';
import type { TaskItem, UserContext } from '../types/domain';

const task: TaskItem = {
  id: 1, projectId: 1, title: 'Test task', description: null, status: 'Todo', priority: 'Medium', dueDate: null,
  assignedUserId: 3, createdByUserId: 1, createdAt: '2026-08-01T00:00:00Z', updatedAt: '2026-08-01T00:00:00Z', completedAt: null,
};
const manager: UserContext = { id: 2, email: 'manager@example.com', workspaceRole: 'Manager', projectRole: 'ProjectManager' };

describe('task transition policy', () => {
  it('matches the backend transition matrix', () => {
    expect(taskTransitions).toEqual({
      Todo: ['InProgress', 'Cancelled'],
      InProgress: ['InReview', 'Cancelled'],
      InReview: ['Done', 'Cancelled'],
      Done: ['InProgress'],
      Cancelled: ['InProgress'],
    });
  });

  it('blocks invalid targets and archived projects', () => {
    expect(canTransitionTask(task, 'Done', manager, false).allowed).toBe(false);
    expect(canTransitionTask(task, 'InProgress', manager, true).allowed).toBe(false);
  });

  it('lets a team member move only an assigned task', () => {
    const member: UserContext = { id: 3, email: 'member@example.com', workspaceRole: 'Member', projectRole: 'TeamMember' };
    expect(canTransitionTask(task, 'InProgress', member, false).allowed).toBe(true);
    expect(canTransitionTask({ ...task, assignedUserId: 4 }, 'InProgress', member, false).allowed).toBe(false);
  });

  it('allows reopening a cancelled task only for managers and owners', () => {
    const cancelled = { ...task, status: 'Cancelled' as const };
    expect(canTransitionTask(cancelled, 'InProgress', manager, false).allowed).toBe(true);
    expect(canTransitionTask(cancelled, 'InProgress', { ...manager, workspaceRole: 'Member', projectRole: 'TeamMember', id: 3 }, false).allowed).toBe(false);
  });
});
