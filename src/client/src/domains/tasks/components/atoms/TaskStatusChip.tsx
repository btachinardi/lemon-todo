import { memo } from 'react';
import { Badge } from '@/ui/badge';
import { cn } from '@/lib/utils';
import { TaskStatus } from '../../types/task.types';

const statusConfig: Record<TaskStatus, { label: string; className: string }> = {
  [TaskStatus.Todo]: { label: 'To Do', className: 'bg-status-todo text-status-todo-foreground' },
  [TaskStatus.InProgress]: { label: 'In Progress', className: 'bg-status-in-progress text-status-in-progress-foreground' },
  [TaskStatus.Done]: { label: 'Done', className: 'bg-status-done text-status-done-foreground' },
};

interface TaskStatusChipProps {
  status: TaskStatus;
  className?: string;
}

/** Colored badge displaying the current task lifecycle status. */
export const TaskStatusChip = memo(function TaskStatusChip({ status, className }: TaskStatusChipProps) {
  const config = statusConfig[status];

  return (
    <Badge variant="outline" className={cn(config.className, className)}>
      {config.label}
    </Badge>
  );
});
