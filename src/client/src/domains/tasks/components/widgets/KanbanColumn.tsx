import { cn } from '@/lib/utils';
import { ScrollArea } from '@/ui/scroll-area';
import type { Column } from '../../types/board.types';
import type { TaskItem } from '../../types/task.types';
import { TaskCard } from './TaskCard';

interface KanbanColumnProps {
  column: Column;
  tasks: TaskItem[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  className?: string;
}

export function KanbanColumn({
  column,
  tasks,
  onCompleteTask,
  onSelectTask,
  className,
}: KanbanColumnProps) {
  return (
    <div className={cn('flex w-72 shrink-0 flex-col rounded-lg bg-muted/50 p-2', className)}>
      <div className="mb-2 flex items-center justify-between px-1">
        <h3 className="text-sm font-semibold">{column.name}</h3>
        <span className="text-xs text-muted-foreground">
          {tasks.length}
          {column.wipLimit != null && `/${column.wipLimit}`}
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
            />
          ))}
          {tasks.length === 0 && (
            <p className="py-8 text-center text-sm text-muted-foreground">No tasks</p>
          )}
        </div>
      </ScrollArea>
    </div>
  );
}
