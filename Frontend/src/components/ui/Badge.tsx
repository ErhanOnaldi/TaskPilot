import type { TaskPriority, TaskStatus } from '../../types/domain';
import { priorityMeta, statusMeta } from '../../lib/formatters';

export function StatusBadge({ status, compact = false }: { status: TaskStatus; compact?: boolean }) {
  const meta = statusMeta[status];
  return <span className={`badge badge--${meta.tone}`}><span aria-hidden="true">{meta.icon}</span>{compact ? null : meta.label}</span>;
}

export function PriorityBadge({ priority, compact = false }: { priority: TaskPriority; compact?: boolean }) {
  const meta = priorityMeta[priority];
  return <span className={`badge badge--${meta.tone}`}><span aria-hidden="true">{meta.icon}</span>{compact ? null : meta.label}</span>;
}

export function LabelChip({ label }: { label: string }) {
  return <span className="label-chip">{label}</span>;
}
