import { memo, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';
import { Badge } from '@/ui/badge';
import { CheckCircle2Icon, InboxIcon } from 'lucide-react';
import type { Task } from '../../types/task.types';
import { TaskStatus, Priority } from '../../types/task.types';
import type { TaskGroup } from '../../types/grouping.types';
import { PriorityBadge } from '../atoms/PriorityBadge';
import { TaskStatusChip } from '../atoms/TaskStatusChip';
import { DueDateLabel } from '../atoms/DueDateLabel';
import { TagList } from '../atoms/TagList';
import { TaskCheckbox } from '../atoms/TaskCheckbox';

const priorityBorder: Record<Priority, string> = {
  [Priority.None]: 'border-l-transparent',
  [Priority.Low]: 'border-l-priority-low-foreground',
  [Priority.Medium]: 'border-l-priority-medium-foreground',
  [Priority.High]: 'border-l-priority-high-foreground',
  [Priority.Critical]: 'border-l-priority-critical-foreground',
};

interface TaskListViewProps {
  groups: TaskGroup[];
  /** When true, renders group headers with labels and counts. @defaultValue false */
  showGroupHeaders?: boolean;
  /** Called when the user toggles a task's completion checkbox. */
  onCompleteTask?: (id: string) => void;
  /** Called when the user clicks a task row to view details. */
  onSelectTask?: (id: string) => void;
  /** ID of the task currently being toggled (shows spinner). */
  togglingTaskId?: string | null;
  className?: string;
}

/**
 * Grouped list view of tasks. Each group renders an optional header,
 * active tasks, and an optional "Completed" separator with done tasks.
 * When `showGroupHeaders` is false (default), group headers are hidden.
 */
export function TaskListView({
  groups,
  showGroupHeaders = false,
  onCompleteTask,
  onSelectTask,
  togglingTaskId,
  className,
}: TaskListViewProps) {
  const { t } = useTranslation();
  const totalTasks = groups.reduce((sum, g) => sum + g.tasks.length + g.completedTasks.length, 0);

  if (totalTasks === 0) {
    return (
      <div className={cn('flex flex-col items-center justify-center gap-3 py-20', className)}>
        <div className="rounded-full bg-secondary p-4">
          <InboxIcon className="size-8 text-muted-foreground/50" />
        </div>
        <div className="text-center">
          <p className="text-lg font-semibold">{t('tasks.empty.listTitle')}</p>
          <p className="mt-1 text-base text-muted-foreground">{t('tasks.empty.listSubtitle')}</p>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('mx-auto w-full max-w-4xl flex-col py-2', className)}>
      {groups.map((group) => {
        const groupTaskCount = group.tasks.length + group.completedTasks.length;
        return (
          <div key={group.key}>
            {showGroupHeaders && (
              <div className="flex items-center gap-2 px-4 pt-5 pb-2">
                <h3 className="text-sm font-semibold uppercase tracking-wider text-muted-foreground">
                  {group.label}
                </h3>
                <Badge variant="secondary" className="px-1.5 py-0 text-[10px]">
                  {groupTaskCount}
                </Badge>
              </div>
            )}

            {group.tasks.map((task, index) => (
              <TaskListItem
                key={task.id}
                task={task}
                index={index}
                togglingTaskId={togglingTaskId}
                onCompleteTask={onCompleteTask}
                onSelectTask={onSelectTask}
              />
            ))}

            {group.completedTasks.length > 0 && (
              <>
                <div className="flex items-center gap-2 px-4 py-2">
                  <div className="h-px flex-1 border-t border-dashed border-border/50" />
                  <span className="flex items-center gap-1.5 text-sm text-muted-foreground">
                    <CheckCircle2Icon className="size-3" />
                    {t('tasks.completed', { count: group.completedTasks.length })}
                  </span>
                  <div className="h-px flex-1 border-t border-dashed border-border/50" />
                </div>
                {group.completedTasks.map((task, index) => (
                  <TaskListItem
                    key={task.id}
                    task={task}
                    index={index}
                    togglingTaskId={togglingTaskId}
                    onCompleteTask={onCompleteTask}
                    onSelectTask={onSelectTask}
                  />
                ))}
              </>
            )}
          </div>
        );
      })}
    </div>
  );
}

/**
 * Props for a single task list item row.
 * @internal
 */
interface TaskListItemProps {
  /** The task to render. */
  task: Task;
  /** Position in the list, used for staggered animation. */
  index: number;
  /** ID of the task currently being toggled (shows spinner). */
  togglingTaskId?: string | null;
  /** Called when the user toggles the completion checkbox. */
  onCompleteTask?: (id: string) => void;
  /** Called when the user clicks the row. */
  onSelectTask?: (id: string) => void;
}

/**
 * Single row in the task list view. Wrapped in `React.memo` because it
 * renders inside `.map()` — without memo, toggling ONE task re-renders
 * ALL rows in the list.
 */
const TaskListItem = memo(function TaskListItem({ task, index, togglingTaskId, onCompleteTask, onSelectTask }: TaskListItemProps) {
  const isDone = task.status === TaskStatus.Done;
  const isToggling = togglingTaskId === task.id;
  const animationStyle = useMemo(() => ({ animationDelay: `${index * 30}ms` }), [index]);

  return (
    <div
      className={cn(
        'animate-fade-in-up border-l-2 border-b border-b-border/60 bg-card/60 transition-all duration-300',
        'hover:bg-secondary/50',
        'focus-visible:bg-secondary/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring',
        priorityBorder[task.priority],
      )}
      style={animationStyle}
      role="button"
      tabIndex={0}
      aria-label={`Task: ${task.title}`}
      onClick={() => onSelectTask?.(task.id)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onSelectTask?.(task.id);
        }
      }}
    >
      <div className="flex cursor-pointer items-center gap-3 px-4 py-3">
        <TaskCheckbox
          checked={isDone}
          onToggle={() => onCompleteTask?.(task.id)}
          isLoading={isToggling}
        />
        <div className="min-w-0 flex-1">
          <p className={cn('truncate text-base font-semibold', isDone && 'line-through opacity-50')}>
            {task.title}
          </p>
          <div className="mt-1 flex flex-wrap items-center gap-2">
            <TaskStatusChip status={task.status} />
            <PriorityBadge priority={task.priority} />
            <DueDateLabel dueDate={task.dueDate} isDone={isDone} />
            <TagList tags={task.tags} />
          </div>
        </div>
      </div>
    </div>
  );
});
