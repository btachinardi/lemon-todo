import { cn } from '@/lib/utils';
import { Separator } from '@/ui/separator';
import { Button } from '@/ui/button';
import { CheckCircle2Icon, InboxIcon, LoaderIcon } from 'lucide-react';
import type { Task } from '../../types/task.types';
import { TaskStatus } from '../../types/task.types';
import { PriorityBadge } from '../atoms/PriorityBadge';
import { TaskStatusChip } from '../atoms/TaskStatusChip';
import { DueDateLabel } from '../atoms/DueDateLabel';
import { TagList } from '../atoms/TagList';

interface TaskListViewProps {
  tasks: Task[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  togglingTaskId?: string | null;
  className?: string;
}

export function TaskListView({ tasks, onCompleteTask, onSelectTask, togglingTaskId, className }: TaskListViewProps) {
  if (tasks.length === 0) {
    return (
      <div className={cn('flex flex-col items-center justify-center gap-3 py-16', className)}>
        <InboxIcon className="size-10 text-muted-foreground/50" />
        <div className="text-center">
          <p className="font-medium">No tasks yet</p>
          <p className="mt-1 text-sm text-muted-foreground">Add a task above to get started.</p>
        </div>
      </div>
    );
  }

  return (
    <div className={cn('mx-auto w-full max-w-4xl flex-col', className)}>
      {tasks.map((task, index) => {
        const isDone = task.status === TaskStatus.Done;
        const isToggling = togglingTaskId === task.id;
        return (
          <div key={task.id}>
            <div
              className="flex cursor-pointer items-center gap-3 px-4 py-3 transition-colors hover:bg-muted/50 focus-visible:bg-muted/50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-ring"
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
              <Button
                variant="ghost"
                size="icon-xs"
                className="shrink-0"
                onClick={(e) => {
                  e.stopPropagation();
                  onCompleteTask?.(task.id);
                }}
                disabled={isToggling}
                aria-label={isDone ? 'Mark as incomplete' : 'Mark as complete'}
              >
                {isToggling ? (
                  <LoaderIcon className="size-4 animate-spin text-muted-foreground" />
                ) : (
                  <CheckCircle2Icon
                    className={cn('size-4', isDone ? 'text-success-foreground' : 'text-muted-foreground')}
                  />
                )}
              </Button>
              <div className="min-w-0 flex-1">
                <p className={cn('truncate text-sm font-medium', isDone && 'line-through opacity-60')}>
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
            {index < tasks.length - 1 && <Separator />}
          </div>
        );
      })}
    </div>
  );
}
