import { describe, expect, it } from 'vitest';
import { formatRelativeDue } from './formatters';

describe('due date semantics', () => {
  const now = new Date('2026-08-02T12:00:00+03:00');

  it('marks only open tasks as overdue', () => {
    expect(formatRelativeDue({ dueDate: '2026-08-01T12:00:00+03:00', status: 'Todo' }, now).overdue).toBe(true);
    expect(formatRelativeDue({ dueDate: '2026-08-01T12:00:00+03:00', status: 'Done' }, now).overdue).toBe(false);
    expect(formatRelativeDue({ dueDate: '2026-08-01T12:00:00+03:00', status: 'Cancelled' }, now).overdue).toBe(false);
  });

  it('describes late completion without marking the task overdue', () => {
    expect(formatRelativeDue({ dueDate: '2026-08-01T12:00:00+03:00', completedAt: '2026-08-02T12:00:00+03:00', status: 'Done' }, now)).toEqual({
      label: '1 Ağu · geç tamamlandı',
      overdue: false,
    });
  });

  it('shows only the date for cancelled tasks', () => {
    expect(formatRelativeDue({ dueDate: '2026-08-01T12:00:00+03:00', completedAt: null, status: 'Cancelled' }, now)).toEqual({
      label: '1 Ağu',
      overdue: false,
    });
  });
});
