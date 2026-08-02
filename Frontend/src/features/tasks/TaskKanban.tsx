import { Plus } from 'lucide-react';
import { useState } from 'react';
import { statusMeta } from '../../lib/formatters';
import { canTransitionTask } from '../../lib/taskRules';
import type { TaskItem, TaskStatus, UserContext } from '../../types/domain';
import { TaskCard } from './TaskCard';

const columns: TaskStatus[] = ['Todo', 'InProgress', 'InReview', 'Done', 'Cancelled'];

export function TaskKanban({ tasks, user, projectArchived, getHref, onTransition }: {
  tasks: TaskItem[];
  user: UserContext;
  projectArchived: boolean;
  getHref: (task: TaskItem) => string;
  onTransition: (task: TaskItem, status: TaskStatus) => void;
}) {
  const [dragged, setDragged] = useState<TaskItem | null>(null);
  return (
    <div className="kanban" aria-label="Görev Kanban panosu">
      {columns.map((status) => {
        const cards = tasks.filter((task) => task.status === status);
        const permission = dragged ? canTransitionTask(dragged, status, user, projectArchived) : null;
        const blocked = dragged && dragged.status !== status && !permission?.allowed;
        return (
          <section className={`kanban-column${blocked ? ' kanban-column--blocked' : ''}`} key={status} onDragOver={(event) => { if (permission?.allowed) event.preventDefault(); }} onDrop={() => { if (dragged && permission?.allowed) onTransition(dragged, status); setDragged(null); }}>
            <header><span className={`tone-dot tone-dot--${statusMeta[status].tone}`}>{statusMeta[status].icon}</span><h2>{statusMeta[status].label}</h2><span>{cards.length}</span>{status === 'Todo' ? <button className="icon-button" aria-label="Yeni görev"><Plus /></button> : null}</header>
            <div className="kanban-column__body">{cards.map((task) => {
              const canDrag = canTransitionTask(task, status === 'Todo' ? 'InProgress' : task.status, user, projectArchived).allowed || user.projectRole === 'ProjectManager';
              return <TaskCard key={task.id} task={task} href={getHref(task)} draggable={canDrag} onDragStart={() => setDragged(task)} onDragEnd={() => setDragged(null)} />;
            })}{!cards.length ? <p>Bu durumda görev yok.</p> : null}</div>
          </section>
        );
      })}
    </div>
  );
}
