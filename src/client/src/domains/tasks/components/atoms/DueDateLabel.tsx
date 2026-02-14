import { cn } from '@/lib/utils';
import { AlertTriangleIcon, CalendarIcon } from 'lucide-react';

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
  } else if (isOverdue) {
    label = `Overdue: ${date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })}`;
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
}
