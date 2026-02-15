import { Card, CardContent, CardHeader, CardTitle } from '@/ui/card';
import { cn } from '@/lib/utils';
import type { Task } from '../../types/task.types';
import { TaskStatus, Priority } from '../../types/task.types';
import { PriorityBadge } from '../atoms/PriorityBadge';
import { DueDateLabel } from '../atoms/DueDateLabel';
import { TagList } from '../atoms/TagList';
import { TaskCheckbox } from '../atoms/TaskCheckbox';

const priorityAccent: Record<Priority, string> = {
  [Priority.None]: 'border-l-border',
  [Priority.Low]: 'border-l-priority-low-foreground',
  [Priority.Medium]: 'border-l-priority-medium-foreground',
  [Priority.High]: 'border-l-priority-high-foreground',
  [Priority.Critical]: 'border-l-priority-critical-foreground',
};

interface TaskCardProps {
  task: Task;
  /** Toggles completion (complete if Todo/InProgress, uncomplete if Done). */
  onComplete?: (id: string) => void;
  /** Fires when the card body is clicked or activated via keyboard. */
  onSelect?: (id: string) => void;
  /** Shows a spinner on the complete button while the mutation is in-flight. */
  isToggling?: boolean;
  /** Visual hint that this card is currently being dragged. */
  isDragging?: boolean;
  /** Visual hint that this is the drag overlay (floating preview). */
  isOverlay?: boolean;
  className?: string;
  style?: React.CSSProperties;
}

/**
 * Compact card used in both kanban columns and list views.
 * Displays title, priority badge, due date, and tags. Completed tasks
 * render with reduced opacity and a strikethrough title.
 */
export function TaskCard({
  task,
  onComplete,
  onSelect,
  isToggling,
  isDragging,
  isOverlay,
  className,
  style,
}: TaskCardProps) {
  const isDone = task.status === TaskStatus.Done;

  return (
    <Card
      className={cn(
        'border-l-2 py-3 transition-all duration-200',
        'hover:bg-card/80 hover:shadow-[0_0_20px_rgba(169,255,3,0.06)]',
        'focus-visible:ring-2 focus-visible:ring-ring',
        priorityAccent[task.priority],
        isDone && 'opacity-50',
        isDragging && 'opacity-30',
        isOverlay && 'rotate-2 shadow-[0_0_30px_rgba(169,255,3,0.15)] ring-1 ring-primary/30',
        !isDragging && 'cursor-pointer',
        className,
      )}
      style={style}
      tabIndex={isDragging ? -1 : 0}
      role="button"
      aria-label={`Task: ${task.title}`}
      onClick={() => !isDragging && onSelect?.(task.id)}
      onKeyDown={(e) => {
        if ((e.key === 'Enter' || e.key === ' ') && !isDragging) {
          e.preventDefault();
          onSelect?.(task.id);
        }
      }}
    >
      <CardHeader className="gap-1 px-3 py-0">
        <div className="flex items-start gap-2">
          <TaskCheckbox
            checked={isDone}
            onToggle={() => onComplete?.(task.id)}
            isLoading={isToggling}
            className="mt-0.5"
          />
          <CardTitle className={cn('text-sm font-medium leading-tight', isDone && 'line-through')}>
            {task.title}
          </CardTitle>
        </div>
      </CardHeader>
      {(task.tags.length > 0 || task.dueDate || task.priority !== 'None') && (
        <CardContent className="px-3 py-0">
          <div className="flex flex-wrap items-center gap-2">
            <PriorityBadge priority={task.priority} />
            <DueDateLabel dueDate={task.dueDate} />
          </div>
          <TagList tags={task.tags} className="mt-1.5" />
        </CardContent>
      )}
    </Card>
  );
}
