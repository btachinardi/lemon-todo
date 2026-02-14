import { cn } from '@/lib/utils';
import { ScrollArea } from '@/ui/scroll-area';
import type { Column } from '../../types/board.types';
import type { Task } from '../../types/task.types';
import { TaskCard } from './TaskCard';

interface KanbanColumnProps {
  column: Column;
  tasks: Task[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  togglingTaskId?: string | null;
  className?: string;
}

export function KanbanColumn({
  column,
  tasks,
  onCompleteTask,
  onSelectTask,
  togglingTaskId,
  className,
}: KanbanColumnProps) {
  return (
    <div className={cn('flex w-72 shrink-0 flex-col rounded-lg bg-muted/50 p-2', className)}>
      <div className="mb-2 flex items-center justify-between px-1">
        <h3 className="text-xs font-semibold uppercase tracking-wide text-muted-foreground">{column.name}</h3>
        <span className="text-xs text-muted-foreground">
          {tasks.length}
          {column.maxTasks != null && `/${column.maxTasks}`}
        </span>
      </div>
      <ScrollArea className="flex-1">
        <div className="flex flex-col gap-2 p-1">
          {tasks.map((task) => (
            <TaskCard
              key={task.id}
              task={task}
              onComplete={onCompleteTask}
              onSelect={onSelectTask}
              isToggling={togglingTaskId === task.id}
            />
          ))}
          {tasks.length === 0 && (
            <div className="flex flex-col items-center gap-1 py-8 text-center">
              <p className="text-sm text-muted-foreground">No tasks</p>
              <p className="text-xs text-muted-foreground/70">Add a task above to get started.</p>
            </div>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
