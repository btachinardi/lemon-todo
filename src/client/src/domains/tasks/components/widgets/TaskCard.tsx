import { Card, CardContent, CardHeader, CardTitle } from '@/ui/card';
import { cn } from '@/lib/utils';
import { CheckCircle2Icon, MoreHorizontalIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import type { TaskItem } from '../../types/task.types';
import { TaskStatus } from '../../types/task.types';
import { PriorityBadge } from '../atoms/PriorityBadge';
import { DueDateLabel } from '../atoms/DueDateLabel';
import { TagList } from '../atoms/TagList';

interface TaskCardProps {
  task: TaskItem;
  onComplete?: (id: string) => void;
  onSelect?: (id: string) => void;
  className?: string;
}

export function TaskCard({ task, onComplete, onSelect, className }: TaskCardProps) {
  const isDone = task.status === TaskStatus.Done;

  return (
    <Card
      className={cn(
        'cursor-pointer transition-shadow hover:shadow-md py-3',
        isDone && 'opacity-60',
        className,
      )}
      onClick={() => onSelect?.(task.id)}
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
            aria-label={isDone ? 'Mark as incomplete' : 'Mark as complete'}
          >
            <CheckCircle2Icon
              className={cn('size-4', isDone ? 'text-green-600' : 'text-muted-foreground')}
            />
          </Button>
          <CardTitle className={cn('text-sm font-medium leading-tight', isDone && 'line-through')}>
            {task.title}
          </CardTitle>
          <Button
            variant="ghost"
            size="icon-xs"
            className="ml-auto shrink-0"
            onClick={(e) => e.stopPropagation()}
            aria-label="Task options"
          >
            <MoreHorizontalIcon className="size-4" />
          </Button>
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
