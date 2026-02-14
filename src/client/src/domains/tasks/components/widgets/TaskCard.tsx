import { Card, CardContent, CardHeader, CardTitle } from '@/ui/card';
import { cn } from '@/lib/utils';
import { CheckCircle2Icon, LoaderIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import type { Task } from '../../types/task.types';
import { TaskStatus } from '../../types/task.types';
import { PriorityBadge } from '../atoms/PriorityBadge';
import { DueDateLabel } from '../atoms/DueDateLabel';
import { TagList } from '../atoms/TagList';

interface TaskCardProps {
  task: Task;
  onComplete?: (id: string) => void;
  onSelect?: (id: string) => void;
  isToggling?: boolean;
  className?: string;
}

export function TaskCard({ task, onComplete, onSelect, isToggling, className }: TaskCardProps) {
  const isDone = task.status === TaskStatus.Done;

  return (
    <Card
      className={cn(
        'cursor-pointer transition-shadow hover:shadow-md focus-visible:ring-2 focus-visible:ring-ring py-3',
        isDone && 'opacity-60',
        className,
      )}
      tabIndex={0}
      role="button"
      aria-label={`Task: ${task.title}`}
      onClick={() => onSelect?.(task.id)}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          e.preventDefault();
          onSelect?.(task.id);
        }
      }}
    >
      <CardHeader className="gap-1 px-3 py-0">
        <div className="flex items-start gap-2">
          <Button
            variant="ghost"
            size="icon-xs"
            className="mt-0.5 shrink-0"
            onClick={(e) => {
              e.stopPropagation();
              onComplete?.(task.id);
            }}
            disabled={isToggling}
            aria-label={isDone ? 'Mark as incomplete' : 'Mark as complete'}
          >
            {isToggling ? (
              <LoaderIcon className="size-4 animate-spin text-muted-foreground" />
            ) : (
              <CheckCircle2Icon
                className={cn('size-4', isDone ? 'text-success-foreground' : 'text-muted-foreground')}
              />
            )}
          </Button>
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
