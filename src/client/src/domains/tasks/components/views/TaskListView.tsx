import { cn } from '@/lib/utils';
import { InboxIcon } from 'lucide-react';
import type { Task } from '../../types/task.types';
import { TaskStatus, Priority } from '../../types/task.types';
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
  tasks: Task[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  togglingTaskId?: string | null;
  className?: string;
}

/**
 * Flat list view of tasks with inline status, priority, due date, and tags.
 * Shows a centered empty state when the task list is empty.
 */
export function TaskListView({ tasks, onCompleteTask, onSelectTask, togglingTaskId, className }: TaskListViewProps) {
  if (tasks.length === 0) {
    return (
      <div className={cn('flex flex-col items-center justify-center gap-3 py-20', className)}>
        <div className="rounded-full bg-secondary p-4">
          <InboxIcon className="size-8 text-muted-foreground/50" />
        </div>
        <div className="text-center">
          <p className="font-display font-semibold">No tasks yet</p>
          <p className="mt-1 text-sm text-muted-foreground">Add a task above to get started.</p>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('mx-auto w-full max-w-4xl flex-col py-2', className)}>
      {tasks.map((task, index) => {
        const isDone = task.status === TaskStatus.Done;
        const isToggling = togglingTaskId === task.id;
        return (
          <div
            key={task.id}
            className={cn(
              'animate-fade-in-up border-l-2 border-b border-b-border/30 transition-colors',
              'hover:bg-secondary/40',
              'focus-visible:bg-secondary/40 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring',
              priorityBorder[task.priority],
            )}
            style={{ animationDelay: `${index * 30}ms` }}
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
                <p className={cn('truncate text-sm font-medium', isDone && 'line-through opacity-50')}>
                  {task.title}
                </p>
                <div className="mt-1 flex flex-wrap items-center gap-2">
                  <TaskStatusChip status={task.status} />
                  <PriorityBadge priority={task.priority} />
                  <DueDateLabel dueDate={task.dueDate} />
                  <TagList tags={task.tags} />
                </div>
              </div>
            </div>
          </div>
        );
      })}
    </div>
  );
}
