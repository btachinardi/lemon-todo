import { Badge } from '@/ui/badge';
import { cn } from '@/lib/utils';
import { TaskStatus } from '../../types/task.types';

const statusConfig: Record<TaskStatus, { label: string; className: string }> = {
  [TaskStatus.Todo]: { label: 'To Do', className: 'bg-slate-100 text-slate-800 dark:bg-slate-800 dark:text-slate-300' },
  [TaskStatus.InProgress]: { label: 'In Progress', className: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300' },
  [TaskStatus.Done]: { label: 'Done', className: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-300' },
};

interface TaskStatusChipProps {
  status: TaskStatus;
  className?: string;
}

export function TaskStatusChip({ status, className }: TaskStatusChipProps) {
  const config = statusConfig[status];

  return (
    <Badge variant="outline" className={cn(config.className, className)}>
      {config.label}
    </Badge>
  );
}
