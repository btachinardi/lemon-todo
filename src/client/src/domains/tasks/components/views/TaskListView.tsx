import { cn } from '@/lib/utils';
import { Separator } from '@/ui/separator';
import type { BoardTask } from '../../types/task.types';
import { PriorityBadge } from '../atoms/PriorityBadge';
import { TaskStatusChip } from '../atoms/TaskStatusChip';
import { DueDateLabel } from '../atoms/DueDateLabel';
import { TagList } from '../atoms/TagList';
import { TaskStatus } from '../../types/task.types';
import { Button } from '@/ui/button';
import { CheckCircle2Icon } from 'lucide-react';

interface TaskListViewProps {
  tasks: BoardTask[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  className?: string;
}

export function TaskListView({ tasks, onCompleteTask, onSelectTask, className }: TaskListViewProps) {
  if (tasks.length === 0) {
    return (
      <div className={cn('flex flex-col items-center justify-center py-16', className)}>
        <p className="text-muted-foreground">No tasks found</p>
      </div>
    );
  }

  return (
    <div className={cn('flex flex-col', className)}>
      {tasks.map((task, index) => {
        const isDone = task.status === TaskStatus.Done;
        return (
          <div key={task.id}>
            <div
              className="flex cursor-pointer items-center gap-3 px-4 py-3 transition-colors hover:bg-muted/50"
              onClick={() => onSelectTask?.(task.id)}
            >
              <Button
                variant="ghost"
                size="icon-xs"
                className="shrink-0"
                onClick={(e) => {
                  e.stopPropagation();
                  onCompleteTask?.(task.id);
                }}
                aria-label={isDone ? 'Mark as incomplete' : 'Mark as complete'}
              >
                <CheckCircle2Icon
                  className={cn('size-4', isDone ? 'text-green-600' : 'text-muted-foreground')}
                />
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
