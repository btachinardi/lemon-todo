import { cn } from '@/lib/utils';
import { CalendarIcon } from 'lucide-react';

interface DueDateLabelProps {
  dueDate: string | null;
  className?: string;
}

export function DueDateLabel({ dueDate, className }: DueDateLabelProps) {
  if (!dueDate) return null;

  const date = new Date(dueDate);
  const now = new Date();
  const isOverdue = date < now;
  const isToday = date.toDateString() === now.toDateString();

  const tomorrow = new Date(now);
  tomorrow.setDate(tomorrow.getDate() + 1);
  const isTomorrow = date.toDateString() === tomorrow.toDateString();

  let label: string;
  if (isToday) {
    label = 'Today';
  } else if (isTomorrow) {
    label = 'Tomorrow';
  } else {
    label = date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }

  return (
    <span
      className={cn(
        'inline-flex items-center gap-1 text-xs',
        isOverdue && !isToday ? 'text-red-600 dark:text-red-400' : 'text-muted-foreground',
        className,
      )}
    >
      <CalendarIcon className="size-3" />
      {label}
    </span>
  );
}
