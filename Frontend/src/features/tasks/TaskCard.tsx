import { GripVertical } from 'lucide-react';
import { Link } from 'react-router-dom';
import { MemberAvatar } from '../../components/ui/Avatar';
import { PriorityBadge } from '../../components/ui/Badge';
import { formatRelativeDue } from '../../lib/formatters';
import type { TaskItem } from '../../types/domain';

export function TaskCard({ task, href, draggable, onDragStart, onDragEnd }: { task: TaskItem; href: string; draggable: boolean; onDragStart: () => void; onDragEnd: () => void }) {
  const due = formatRelativeDue(task);
  return (
    <article className="task-card" draggable={draggable} onDragStart={onDragStart} onDragEnd={onDragEnd}>
      <header><span className="mono">TP-{task.id}</span>{draggable ? <GripVertical aria-label="Kart sürüklenebilir" /> : null}</header>
      <Link to={href}>{task.title}</Link>
      <footer><PriorityBadge priority={task.priority} compact />{task.assignedUserId ? <MemberAvatar name={`Kullanıcı ${task.assignedUserId}`} size="sm" /> : <span className="unassigned">Atanmamış</span>}<time className={due.overdue ? 'is-overdue' : ''}>{due.label}</time></footer>
    </article>
  );
}
