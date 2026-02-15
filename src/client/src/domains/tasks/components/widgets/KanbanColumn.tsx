import { memo, useMemo } from 'react';
import { cn } from '@/lib/utils';
import { useDroppable } from '@dnd-kit/core';
import { SortableContext, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { ScrollArea } from '@/ui/scroll-area';
import type { Column } from '../../types/board.types';
import type { Task } from '../../types/task.types';
import { SortableTaskCard } from './SortableTaskCard';

interface KanbanColumnProps {
  column: Column;
  /** Tasks already filtered and sorted for this column. */
  tasks: Task[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  /** ID of the task whose complete button should show a spinner. */
  togglingTaskId?: string | null;
  className?: string;
  style?: React.CSSProperties;
}

/**
 * Vertical lane in the kanban board. Droppable zone with sortable task cards.
 *
 * Wrapped in `React.memo` — rendered inside `.map()` in KanbanBoard.
 * Each column only re-renders when its own tasks or props change.
 */
export const KanbanColumn = memo(function KanbanColumn({
  column,
  tasks,
  onCompleteTask,
  onSelectTask,
  togglingTaskId,
  className,
  style,
}: KanbanColumnProps) {
  const { setNodeRef, isOver } = useDroppable({ id: column.id });
  const taskIds = useMemo(() => tasks.map((t) => t.id), [tasks]);

  return (
    <div
      className={cn(
        'flex min-w-[85vw] flex-1 flex-col snap-center rounded-xl border border-border/40 bg-secondary p-3 transition-all duration-300 sm:min-w-72',
        isOver && 'border-primary/30 bg-primary/5 ring-1 ring-primary/20',
        className,
      )}
      style={style}
    >
      <div className="mb-3 flex items-center justify-between px-1">
        <h3 className="text-xs font-bold uppercase tracking-wider text-secondary-foreground">
          {column.name}
        </h3>
        <span className="inline-flex min-w-5 items-center justify-center rounded-md bg-border px-1.5 py-0.5 text-[10px] font-bold tabular-nums text-muted-foreground">
          {tasks.length}
          {column.maxTasks != null && `/${column.maxTasks}`}
        </span>
      </div>
      <ScrollArea className="flex-1">
        <SortableContext items={taskIds} strategy={verticalListSortingStrategy}>
          <div ref={setNodeRef} className="flex min-h-[60px] flex-col gap-2 p-0.5">
            {tasks.map((task) => (
              <SortableTaskCard
                key={task.id}
                task={task}
                onComplete={onCompleteTask}
                onSelect={onSelectTask}
                isToggling={togglingTaskId === task.id}
              />
            ))}
            {tasks.length === 0 && (
              <div className="flex flex-col items-center gap-1 rounded-lg border border-dashed border-border py-8 text-center">
                <p className="text-sm text-muted-foreground">No tasks</p>
              </div>
            )}
          </div>
        </SortableContext>
      </ScrollArea>
    </div>
  );
});
