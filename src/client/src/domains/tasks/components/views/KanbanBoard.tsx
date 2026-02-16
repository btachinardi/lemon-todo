import { useState, useCallback, useEffect, useRef, useMemo } from 'react';
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

/** Stable drop animation config — defined outside the component to avoid creating a new object on every render. */
const DROP_ANIMATION = { duration: 200, easing: 'ease-out' } as const;

/** Builds a map of columnId -> taskId[] sorted by rank from the board's cards. */
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
  // Sort within each column by card rank
  const cardMap = new Map((board.cards ?? []).map((c) => [c.taskId, c]));
  for (const colId of Object.keys(items)) {
    items[colId].sort((a, b) => (cardMap.get(a)?.rank ?? 0) - (cardMap.get(b)?.rank ?? 0));
  }
  return items;
}

/** Finds the column ID containing the given task. Returns null if not found. */
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
  /** Called when the user toggles a task's completion checkbox. */
  onCompleteTask?: (id: string) => void;
  /** Called when the user clicks a task card to view details. */
  onSelectTask?: (id: string) => void;
  /** Called when a card is dropped at a new position. Passes neighbor IDs for rank computation. */
  onMoveTask?: (taskId: string, columnId: string, previousTaskId: string | null, nextTaskId: string | null) => void;
  /** ID of the task currently being toggled (shows spinner). */
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
  const sortedColumns = useMemo(
    () => [...board.columns].sort((a, b) => a.position - b.position),
    [board.columns],
  );
  const tasksById = useMemo(
    () => new Map(tasks.map((task) => [task.id, task])),
    [tasks],
  );

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
        // Cross-column move — handleDragOver placed the card at the initial
        // crossing point. Use over.id to determine the actual drop position.
        const overId = over.id as string;
        let items = columnItems[currentCol] ?? [];
        const oldIndex = items.indexOf(activeTaskId);
        const newIndex = items.indexOf(overId);

        if (oldIndex !== -1 && newIndex !== -1 && oldIndex !== newIndex) {
          items = arrayMove(items, oldIndex, newIndex);
          setColumnItems((prev) => ({ ...prev, [currentCol]: items }));
        }

        const finalIdx = items.indexOf(activeTaskId);
        const previousTaskId = finalIdx > 0 ? items[finalIdx - 1] : null;
        const nextTaskId = finalIdx < items.length - 1 ? items[finalIdx + 1] : null;
        onMoveTask?.(activeTaskId, currentCol, previousTaskId, nextTaskId);
      } else {
        // Same-column reorder
        const overId = over.id as string;
        const items = columnItems[currentCol];
        const oldIndex = items.indexOf(activeTaskId);
        const newIndex = items.indexOf(overId);

        if (oldIndex !== -1 && newIndex !== -1 && oldIndex !== newIndex) {
          const reordered = arrayMove(items, oldIndex, newIndex);
          setColumnItems((prev) => ({ ...prev, [currentCol]: reordered }));
          const finalIdx = reordered.indexOf(activeTaskId);
          const previousTaskId = finalIdx > 0 ? reordered[finalIdx - 1] : null;
          const nextTaskId = finalIdx < reordered.length - 1 ? reordered[finalIdx + 1] : null;
          onMoveTask?.(activeTaskId, currentCol, previousTaskId, nextTaskId);
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

  // Pre-compute tasks for each column. Keyed on columnItems + tasksById so it
  // only recalculates when drag state or server data changes — not on every render.
  const columnTasksMap = useMemo(() => {
    const map: Record<string, Task[]> = {};
    for (const column of sortedColumns) {
      const taskIdsInColumn = columnItems[column.id] ?? [];
      map[column.id] = taskIdsInColumn
        .map((id) => tasksById.get(id))
        .filter((t): t is Task => t != null)
        .map((t) =>
          t.status !== column.targetStatus ? { ...t, status: column.targetStatus } : t,
        );
    }
    return map;
  }, [sortedColumns, columnItems, tasksById]);

  // Stable animation delay styles for each column (only 3 columns, so this is fine).
  const columnAnimationStyles = useMemo(
    () => sortedColumns.map((_, index) => ({ animationDelay: `${index * 100}ms` })),
    [sortedColumns],
  );

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
        <div className="flex snap-x snap-mandatory gap-4 overflow-x-auto p-4 sm:snap-none sm:p-6">
          {sortedColumns.map((column, index) => (
              <KanbanColumn
                key={column.id}
                column={column}
                tasks={columnTasksMap[column.id] ?? []}
                onCompleteTask={onCompleteTask}
                onSelectTask={onSelectTask}
                togglingTaskId={togglingTaskId}
                className="animate-fade-in-up"
                style={columnAnimationStyles[index]}
              />
            ))}
        </div>
        <ScrollBar orientation="horizontal" />
      </ScrollArea>

      {/* Floating drag preview */}
      <DragOverlay dropAnimation={DROP_ANIMATION}>
        {activeTask ? (
          <div className="w-80">
            <TaskCard task={activeTask} isOverlay />
          </div>
        ) : null}
      </DragOverlay>
    </DndContext>
  );
}
