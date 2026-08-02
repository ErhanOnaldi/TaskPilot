import { ChevronDown } from 'lucide-react';
import { canTransitionTask, taskTransitions } from '../../lib/taskRules';
import { statusMeta } from '../../lib/formatters';
import type { TaskItem, TaskStatus, UserContext } from '../../types/domain';

export function TaskStatusTransitionMenu({ task, user, projectArchived, onTransition, disabled }: {
  task: TaskItem;
  user: UserContext;
  projectArchived: boolean;
  onTransition: (status: TaskStatus) => void;
  disabled?: boolean;
}) {
  return (
    <details className="transition-menu">
      <summary className="button button--secondary" aria-label={`${task.id} numaralı görevin durumunu değiştir`}>Durumu değiştir <ChevronDown /></summary>
      <div className="transition-menu__list">
        {taskTransitions[task.status].map((target) => {
          const permission = canTransitionTask(task, target, user, projectArchived);
          return <button key={target} disabled={disabled || !permission.allowed} title={permission.reason} onClick={() => onTransition(target)}><span aria-hidden="true">{statusMeta[target].icon}</span><span>{statusMeta[target].label}</span></button>;
        })}
      </div>
    </details>
  );
}
