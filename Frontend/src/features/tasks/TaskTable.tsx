import { Link } from 'react-router-dom';
import { MemberAvatar } from '../../components/ui/Avatar';
import { PriorityBadge, StatusBadge } from '../../components/ui/Badge';
import { formatRelativeDue } from '../../lib/formatters';
import type { TaskItem } from '../../types/domain';

export function TaskTable({ tasks, getHref }: { tasks: TaskItem[]; getHref: (task: TaskItem) => string }) {
  return (
    <div className="task-table-wrap">
      <table className="task-table">
        <thead><tr><th>Görev</th><th>Durum</th><th>Öncelik</th><th>Atanan</th><th>Due date</th><th>Güncellendi</th></tr></thead>
        <tbody>{tasks.map((task) => { const due = formatRelativeDue(task); return <tr key={task.id}><td><Link to={getHref(task)}><span className="mono">TP-{task.id}</span><strong>{task.title}</strong></Link></td><td><StatusBadge status={task.status} /></td><td><PriorityBadge priority={task.priority} /></td><td>{task.assignedUserId ? <span className="member-cell"><MemberAvatar name={`Kullanıcı ${task.assignedUserId}`} size="sm" />Kullanıcı {task.assignedUserId}</span> : <span className="unassigned">Atanmamış</span>}</td><td><time className={due.overdue ? 'is-overdue' : ''}>{due.label}</time></td><td>{new Intl.RelativeTimeFormat('tr', { numeric: 'auto' }).format(-1, 'day')}</td></tr>; })}</tbody>
      </table>
      <div className="mobile-task-list">{tasks.map((task) => { const due = formatRelativeDue(task); return <Link key={task.id} to={getHref(task)}><span className="mono">TP-{task.id}</span><strong>{task.title}</strong><div><StatusBadge status={task.status} /><PriorityBadge priority={task.priority} /><time className={due.overdue ? 'is-overdue' : ''}>{due.label}</time></div></Link>; })}</div>
    </div>
  );
}
