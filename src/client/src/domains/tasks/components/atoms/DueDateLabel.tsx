import { memo } from 'react';
import { useTranslation } from 'react-i18next';
import { cn } from '@/lib/utils';
import { AlertTriangleIcon, CalendarIcon } from 'lucide-react';

interface DueDateLabelProps {
  /** ISO 8601 date string or null. */
  dueDate: string | null;
  /** When true, suppress overdue styling (completed tasks aren't overdue). */
  isDone?: boolean;
  className?: string;
}

/**
 * Displays a human-friendly due date with contextual styling.
 * Shows "Today", "Tomorrow", or "Overdue: ..." with a warning icon.
 * Renders nothing when `dueDate` is null.
 */
export const DueDateLabel = memo(function DueDateLabel({ dueDate, isDone, className }: DueDateLabelProps) {
  const { t } = useTranslation();

  if (!dueDate) return null;

  const date = new Date(dueDate);
  const now = new Date();
  const isOverdue = !isDone && date < now;
  const isToday = date.toDateString() === now.toDateString();

  const tomorrow = new Date(now);
  tomorrow.setDate(tomorrow.getDate() + 1);
  const isTomorrow = date.toDateString() === tomorrow.toDateString();

  let label: string;
  if (isToday) {
    label = t('tasks.dueDate.today');
  } else if (isTomorrow) {
    label = t('tasks.dueDate.tomorrow');
  } else if (isOverdue) {
    label = t('tasks.dueDate.overdue', { date: date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }) });
  } else {
    label = date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }

  const Icon = isOverdue && !isToday ? AlertTriangleIcon : CalendarIcon;

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 text-xs',
        isOverdue && !isToday ? 'text-destructive font-medium' : 'text-muted-foreground',
        className,
      )}
    >
      <Icon className="size-3" />
      {label}
    </span>
  );
});
