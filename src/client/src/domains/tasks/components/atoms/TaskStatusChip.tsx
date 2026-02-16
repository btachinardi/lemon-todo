import { memo } from 'react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/ui/badge';
import { cn } from '@/lib/utils';
import { TaskStatus } from '../../types/task.types';

const statusStyles: Record<TaskStatus, string> = {
  [TaskStatus.Todo]: 'bg-status-todo text-status-todo-foreground',
  [TaskStatus.InProgress]: 'bg-status-in-progress text-status-in-progress-foreground',
  [TaskStatus.Done]: 'bg-status-done text-status-done-foreground',
};

const statusKeys: Record<TaskStatus, string> = {
  [TaskStatus.Todo]: 'tasks.status.todo',
  [TaskStatus.InProgress]: 'tasks.status.inProgress',
  [TaskStatus.Done]: 'tasks.status.done',
};

interface TaskStatusChipProps {
  status: TaskStatus;
  className?: string;
}

/** Colored badge displaying the current task lifecycle status. */
export const TaskStatusChip = memo(function TaskStatusChip({ status, className }: TaskStatusChipProps) {
  const { t } = useTranslation();

  return (
    <Badge variant="outline" className={cn(statusStyles[status], className)}>
      {t(statusKeys[status])}
    </Badge>
  );
});
