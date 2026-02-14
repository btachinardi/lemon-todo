import { ScrollArea, ScrollBar } from '@/ui/scroll-area';
import { cn } from '@/lib/utils';
import type { Board } from '../../types/board.types';
import type { TaskItem } from '../../types/task.types';
import { KanbanColumn } from '../widgets/KanbanColumn';

interface KanbanBoardProps {
  board: Board;
  tasks: TaskItem[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  className?: string;
}

export function KanbanBoard({
  board,
  tasks,
  onCompleteTask,
  onSelectTask,
  className,
}: KanbanBoardProps) {
  const sortedColumns = [...board.columns].sort((a, b) => a.position - b.position);

  const tasksByColumn = new Map<string, TaskItem[]>();
  for (const column of sortedColumns) {
    tasksByColumn.set(column.id, []);
  }
  for (const task of tasks) {
    if (task.columnId && tasksByColumn.has(task.columnId)) {
      tasksByColumn.get(task.columnId)!.push(task);
    }
  }
  for (const columnTasks of tasksByColumn.values()) {
    columnTasks.sort((a, b) => a.position - b.position);
  }

  return (
    <ScrollArea className={cn('w-full', className)}>
      <div className="flex gap-4 p-4">
        {sortedColumns.map((column) => (
          <KanbanColumn
            key={column.id}
            column={column}
            tasks={tasksByColumn.get(column.id) ?? []}
            onCompleteTask={onCompleteTask}
            onSelectTask={onSelectTask}
          />
        ))}
      </div>
      <ScrollBar orientation="horizontal" />
    </ScrollArea>
  );
}
