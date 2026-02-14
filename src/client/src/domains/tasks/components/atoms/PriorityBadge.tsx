import { Badge } from '@/ui/badge';
import { cn } from '@/lib/utils';
import { Priority } from '../../types/task.types';

const priorityConfig: Record<Priority, { label: string; className: string }> = {
  [Priority.None]: { label: 'None', className: '' },
  [Priority.Low]: { label: 'Low', className: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-300' },
  [Priority.Medium]: { label: 'Medium', className: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-300' },
  [Priority.High]: { label: 'High', className: 'bg-orange-100 text-orange-800 dark:bg-orange-900 dark:text-orange-300' },
  [Priority.Critical]: { label: 'Critical', className: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-300' },
};

interface PriorityBadgeProps {
  priority: Priority;
  className?: string;
}

export function PriorityBadge({ priority, className }: PriorityBadgeProps) {
  if (priority === Priority.None) return null;

  const config = priorityConfig[priority];

  return (
    <Badge variant="outline" className={cn(config.className, className)}>
      {config.label}
    </Badge>
  );
}
