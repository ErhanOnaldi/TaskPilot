import type { TaskPriority, TaskStatus } from '../types/domain';

export const statusMeta: Record<TaskStatus, { label: string; icon: string; tone: string }> = {
  Todo: { label: 'Yapılacak', icon: '○', tone: 'neutral' },
  InProgress: { label: 'Devam ediyor', icon: '◐', tone: 'info' },
  InReview: { label: 'İncelemede', icon: '◈', tone: 'warning' },
  Done: { label: 'Tamamlandı', icon: '●', tone: 'success' },
  Cancelled: { label: 'İptal edildi', icon: '⊘', tone: 'danger' },
};

export const priorityMeta: Record<TaskPriority, { label: string; icon: string; tone: string }> = {
  Low: { label: 'Düşük', icon: '▁', tone: 'neutral' },
  Medium: { label: 'Orta', icon: '▃', tone: 'info' },
  High: { label: 'Yüksek', icon: '▅', tone: 'warning' },
  Critical: { label: 'Kritik', icon: '▇', tone: 'danger' },
};

export function formatDate(value: string | null, options?: Intl.DateTimeFormatOptions) {
  if (!value) return '—';
  return new Intl.DateTimeFormat('tr-TR', options ?? { day: 'numeric', month: 'short', year: 'numeric' }).format(new Date(value));
}

export function formatRelativeDue(task: { dueDate: string | null; status: TaskStatus; completedAt?: string | null }, now = new Date()) {
  if (!task.dueDate) return { label: 'Tarih yok', overdue: false };
  const due = new Date(task.dueDate);
  const overdue = ['Todo', 'InProgress', 'InReview'].includes(task.status) && due.getTime() < now.getTime();
  const completedLate = task.status === 'Done' && task.completedAt && new Date(task.completedAt).getTime() > due.getTime();
  const qualifier = overdue ? ' · gecikmiş' : completedLate ? ' · geç tamamlandı' : '';
  return { label: `${formatDate(task.dueDate, { day: 'numeric', month: 'short' })}${qualifier}`, overdue };
}

export function initials(emailOrName: string) {
  return emailOrName
    .split(/[\s@._-]+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase())
    .join('');
}
