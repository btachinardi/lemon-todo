import { useState, useCallback, useEffect, useRef } from 'react';
import {
  DndContext,
  DragOverlay,
  closestCorners,
  PointerSensor,
  useSensor,
  useSensors,
  type DragStartEvent,
  type DragOverEvent,
  type DragEndEvent,
} from '@dnd-kit/core';
import { arrayMove } from '@dnd-kit/sortable';
import { ScrollArea, ScrollBar } from '@/ui/scroll-area';
import { cn } from '@/lib/utils';
import type { Board } from '../../types/board.types';
import type { Task } from '../../types/task.types';
import { KanbanColumn } from '../widgets/KanbanColumn';
import { TaskCard } from '../widgets/TaskCard';

/** Map of columnId -> taskId[] representing the current visual order. */
type ColumnItems = Record<string, string[]>;

function buildColumnItems(board: Board): ColumnItems {
  const items: ColumnItems = {};
  for (const col of board.columns) {
    items[col.id] = [];
  }
  for (const card of board.cards ?? []) {
    if (items[card.columnId]) {
      items[card.columnId].push(card.taskId);
    }
  }
  // Sort within each column by card position
  const cardMap = new Map((board.cards ?? []).map((c) => [c.taskId, c]));
  for (const colId of Object.keys(items)) {
    items[colId].sort((a, b) => (cardMap.get(a)?.position ?? 0) - (cardMap.get(b)?.position ?? 0));
  }
  return items;
}

function findColumnOfTask(items: ColumnItems, taskId: string): string | null {
  for (const [colId, taskIds] of Object.entries(items)) {
    if (taskIds.includes(taskId)) return colId;
  }
  return null;
}

interface KanbanBoardProps {
  board: Board;
  /** Full task list; the board's `cards` are used to sort them into columns. */
  tasks: Task[];
  onCompleteTask?: (id: string) => void;
  onSelectTask?: (id: string) => void;
  /** Called when a card is dropped at a new position. */
  onMoveTask?: (taskId: string, columnId: string, position: number) => void;
  togglingTaskId?: string | null;
  className?: string;
}

/**
 * Horizontally scrollable kanban board with drag-and-drop support.
 * Cards can be reordered within columns and moved across columns.
 * Uses @dnd-kit for accessible, animated drag interactions.
 */
export function KanbanBoard({
  board,
  tasks,
  onCompleteTask,
  onSelectTask,
  onMoveTask,
  togglingTaskId,
  className,
}: KanbanBoardProps) {
  const sortedColumns = [...board.columns].sort((a, b) => a.position - b.position);
  const tasksById = new Map(tasks.map((task) => [task.id, task]));

  // Local state for card positions (enables optimistic drag preview)
  const [columnItems, setColumnItems] = useState<ColumnItems>(() => buildColumnItems(board));
  const [activeId, setActiveId] = useState<string | null>(null);

  // Track the column the card was in when the drag started. A ref is used
  // because handleDragOver updates columnItems (and thus recreates handlers)
  // before handleDragEnd fires — the ref is immune to stale closures.
  const dragOriginColRef = useRef<string | null>(null);

  // Re-sync with server state when board data changes
  useEffect(() => {
    setColumnItems(buildColumnItems(board));
  }, [board]);

  // Require 5px movement before activating drag (prevents accidental drags on click)
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
  );

  const handleDragStart = useCallback((event: DragStartEvent) => {
    const taskId = event.active.id as string;
    setActiveId(taskId);
    dragOriginColRef.current = findColumnOfTask(columnItems, taskId);
  }, [columnItems]);

  const handleDragOver = useCallback((event: DragOverEvent) => {
    const { active, over } = event;
    if (!over) return;

    const activeTaskId = active.id as string;
    const overId = over.id as string;

    const activeCol = findColumnOfTask(columnItems, activeTaskId);
    // overId could be a task or a column (empty column drop target)
    const overCol = findColumnOfTask(columnItems, overId) ?? overId;

    if (!activeCol || !overCol || activeCol === overCol) return;

    // Move card to the new column
    setColumnItems((prev) => {
      const activeItems = [...(prev[activeCol] ?? [])];
      const overItems = [...(prev[overCol] ?? [])];

      const activeIndex = activeItems.indexOf(activeTaskId);
      if (activeIndex === -1) return prev;
      activeItems.splice(activeIndex, 1);

      const overIndex = overItems.indexOf(overId);
      overItems.splice(overIndex >= 0 ? overIndex + 1 : overItems.length, 0, activeTaskId);

      return { ...prev, [activeCol]: activeItems, [overCol]: overItems };
    });
  }, [columnItems]);

  const handleDragEnd = useCallback(
    (event: DragEndEvent) => {
      const { active, over } = event;
      setActiveId(null);

      const originCol = dragOriginColRef.current;
      dragOriginColRef.current = null;

      if (!over || !originCol) return;

      const activeTaskId = active.id as string;
      const currentCol = findColumnOfTask(columnItems, activeTaskId);

      if (!currentCol) return;

      if (originCol !== currentCol) {
        // Cross-column move — handleDragOver already updated local state,
        // now persist via the callback.
        const newPosition = (columnItems[currentCol] ?? []).indexOf(activeTaskId);
        onMoveTask?.(activeTaskId, currentCol, Math.max(0, newPosition));
      } else {
        // Same-column reorder
        const overId = over.id as string;
        const items = columnItems[currentCol];
        const oldIndex = items.indexOf(activeTaskId);
        const newIndex = items.indexOf(overId);

        if (oldIndex !== -1 && newIndex !== -1 && oldIndex !== newIndex) {
          const reordered = arrayMove(items, oldIndex, newIndex);
          setColumnItems((prev) => ({ ...prev, [currentCol]: reordered }));
          onMoveTask?.(activeTaskId, currentCol, reordered.indexOf(activeTaskId));
        }
      }
    },
    [columnItems, onMoveTask],
  );

  const handleDragCancel = useCallback(() => {
    setActiveId(null);
    setColumnItems(buildColumnItems(board));
  }, [board]);

  const activeTask = activeId ? tasksById.get(activeId) ?? null : null;

  return (
    <DndContext
      sensors={sensors}
      collisionDetection={closestCorners}
      onDragStart={handleDragStart}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
      onDragCancel={handleDragCancel}
    >
      <ScrollArea className={cn('w-full', className)}>
        <div className="flex gap-5 p-6">
          {sortedColumns.map((column, index) => {
            const taskIdsInColumn = columnItems[column.id] ?? [];
            const columnTasks = taskIdsInColumn
              .map((id) => tasksById.get(id))
              .filter((t): t is Task => t != null)
              .map((t) =>
                t.status !== column.targetStatus ? { ...t, status: column.targetStatus } : t,
              );

            return (
              <KanbanColumn
                key={column.id}
                column={column}
                tasks={columnTasks}
                onCompleteTask={onCompleteTask}
                onSelectTask={onSelectTask}
                togglingTaskId={togglingTaskId}
                className="animate-fade-in-up"
                style={{ animationDelay: `${index * 100}ms` }}
              />
            );
          })}
        </div>
        <ScrollBar orientation="horizontal" />
      </ScrollArea>

      {/* Floating drag preview */}
      <DragOverlay dropAnimation={{ duration: 200, easing: 'ease-out' }}>
        {activeTask ? (
          <div className="w-80">
            <TaskCard task={activeTask} isOverlay />
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}
