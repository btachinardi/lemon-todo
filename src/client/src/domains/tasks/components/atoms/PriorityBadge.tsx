import { Badge } from '@/ui/badge';
import { cn } from '@/lib/utils';
import { Priority } from '../../types/task.types';

const priorityConfig: Record<Priority, { label: string; className: string }> = {
  [Priority.None]: { label: 'None', className: '' },
  [Priority.Low]: { label: 'Low', className: 'bg-priority-low text-priority-low-foreground' },
  [Priority.Medium]: { label: 'Medium', className: 'bg-priority-medium text-priority-medium-foreground' },
  [Priority.High]: { label: 'High', className: 'bg-priority-high text-priority-high-foreground' },
  [Priority.Critical]: { label: 'Critical', className: 'bg-priority-critical text-priority-critical-foreground' },
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
