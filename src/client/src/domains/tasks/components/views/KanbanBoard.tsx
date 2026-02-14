import { ScrollArea, ScrollBar } from '@/ui/scroll-area';
import { cn } from '@/lib/utils';
import type { Board } from '../../types/board.types';
import type { Task } from '../../types/task.types';
import { KanbanColumn } from '../widgets/KanbanColumn';

interface KanbanBoardProps {
  board: Board;
  tasks: Task[];
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

  // Build a map of taskId → card for quick lookup
  const cardByTaskId = new Map(
    (board.cards ?? []).map((card) => [card.taskId, card]),
  );

  // Build a map of tasks by ID for quick lookup
  const tasksById = new Map(tasks.map((task) => [task.id, task]));

  const tasksByColumn = new Map<string, Task[]>();
  for (const column of sortedColumns) {
    tasksByColumn.set(column.id, []);
  }

  // Place tasks into columns using the board's cards
  for (const card of board.cards ?? []) {
    const task = tasksById.get(card.taskId);
    if (task && tasksByColumn.has(card.columnId)) {
      tasksByColumn.get(card.columnId)!.push(task);
    }
  }

  // Sort tasks within each column by position
  for (const [, columnTasks] of tasksByColumn) {
    columnTasks.sort((a, b) => {
      const cardA = cardByTaskId.get(a.id);
      const cardB = cardByTaskId.get(b.id);
      return (cardA?.position ?? 0) - (cardB?.position ?? 0);
    });
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
