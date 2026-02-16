import { memo } from 'react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/ui/badge';
import { cn } from '@/lib/utils';
import { Priority } from '../../types/task.types';

const priorityStyles: Record<Priority, string> = {
  [Priority.None]: '',
  [Priority.Low]: 'bg-priority-low text-priority-low-foreground',
  [Priority.Medium]: 'bg-priority-medium text-priority-medium-foreground',
  [Priority.High]: 'bg-priority-high text-priority-high-foreground',
  [Priority.Critical]: 'bg-priority-critical text-priority-critical-foreground',
};

const priorityKeys: Record<Priority, string> = {
  [Priority.None]: 'tasks.priority.none',
  [Priority.Low]: 'tasks.priority.low',
  [Priority.Medium]: 'tasks.priority.medium',
  [Priority.High]: 'tasks.priority.high',
  [Priority.Critical]: 'tasks.priority.critical',
};

interface PriorityBadgeProps {
  priority: Priority;
  className?: string;
}

/**
 * Colored badge indicating task priority. Renders nothing for `Priority.None`.
 * Memoized — rendered in task card lists.
 */
export const PriorityBadge = memo(function PriorityBadge({ priority, className }: PriorityBadgeProps) {
  const { t } = useTranslation();

  if (priority === Priority.None) return null;

  return (
    <Badge variant="outline" className={cn(priorityStyles[priority], className)}>
      {t(priorityKeys[priority])}
    </Badge>
  );
});
