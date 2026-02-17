import { useState, useCallback, useEffect, useRef, useMemo } from 'react';
import {
  DndContext,
  DragOverlay,
  closestCorners,
  PointerSensor,
  TouchSensor,
  useSensor,
  useSensors,
  type DragStartEvent,
  type DragMoveEvent,
  type DragOverEvent,
  type DragEndEvent,
} from '@dnd-kit/core';
import { arrayMove } from '@dnd-kit/sortable';
import { cn } from '@/lib/utils';
import type { Board } from '../../types/board.types';
import type { Task } from '../../types/task.types';
import { KanbanColumn } from '../widgets/KanbanColumn';
import { TaskCard } from '../widgets/TaskCard';

/** Map of columnId -> taskId[] representing the current visual order. */
type ColumnItems = Record<string, string[]>;

/** Stable drop animation config — defined outside the component to avoid creating a new object on every render. */
const DROP_ANIMATION = { duration: 200, easing: 'ease-out' } as const;

/** Pixels from the viewport edge that trigger column-snap auto-scroll during drag. */
const EDGE_THRESHOLD_PX = 60;
/** Minimum ms between column snaps — allows the smooth scroll animation to settle. */
const SNAP_COOLDOWN_MS = 400;
/** Minimum horizontal distance (px) from the last snap point to trigger the next snap. */
const SNAP_DISTANCE_PX = 80;
/** Tailwind `sm` breakpoint — column snap only applies below this width. */
const SM_BREAKPOINT = 640;

/** Determines which column is currently centered in the scroll container. */
function getCurrentColumnIndex(container: HTMLElement): number {
  const scrollCenter = container.scrollLeft + container.clientWidth / 2;
  const flexContainer = container.firstElementChild;
  if (!flexContainer) return 0;

  let closestIndex = 0;
  let closestDistance = Infinity;

  for (let i = 0; i < flexContainer.children.length; i++) {
    const child = flexContainer.children[i] as HTMLElement;
    const childCenter = child.offsetLeft + child.offsetWidth / 2;
    const distance = Math.abs(scrollCenter - childCenter);
    if (distance < closestDistance) {
      closestDistance = distance;
      closestIndex = i;
    }
  }

  return closestIndex;
}

/** Smoothly scrolls to center a specific column in the scroll container. */
function scrollToColumn(container: HTMLElement, columnIndex: number): void {
  const flexContainer = container.firstElementChild;
  if (!flexContainer) return;

  const column = flexContainer.children[columnIndex] as HTMLElement | undefined;
  if (!column) return;

  const scrollTarget = column.offsetLeft - (container.clientWidth - column.offsetWidth) / 2;
  container.scrollTo({ left: Math.max(0, scrollTarget), behavior: 'smooth' });
}

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

  // Column-snap auto-scroll state (mobile only)
  const scrollContainerRef = useRef<HTMLDivElement>(null);
  const initialPointerRef = useRef<{ x: number } | null>(null);
  const lastSnapTimeRef = useRef(0);
  const currentColumnIndexRef = useRef(0);
  const snapAnchorXRef = useRef(0);
  const hasLeftEdgeZoneRef = useRef(true);

  // Disable dnd-kit's built-in auto-scroll for the horizontal board container.
  // Vertical auto-scroll within column ScrollAreas is still allowed.
  // We replace horizontal auto-scroll with custom column-snap logic in handleDragMove.
  const autoScrollConfig = useMemo(() => ({
    canScroll: (element: Element) => element !== scrollContainerRef.current,
  }), []);

  // Re-sync with server state when board data changes
  useEffect(() => {
    setColumnItems(buildColumnItems(board));
  }, [board]);

  // PointerSensor for desktop (5px distance prevents accidental drags on click).
  // TouchSensor for mobile (250ms delay lets quick swipes scroll normally;
  // press-and-hold initiates drag; 5px tolerance forgives small finger movement).
  const sensors = useSensors(
    useSensor(PointerSensor, { activationConstraint: { distance: 5 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 5 } }),
  );

  // Track whether a drag is active to disable snap-scroll during drag.
  const isDragging = activeId != null;

  const handleDragStart = useCallback((event: DragStartEvent) => {
    const taskId = event.active.id as string;
    setActiveId(taskId);
    dragOriginColRef.current = findColumnOfTask(columnItems, taskId);

    // Capture initial pointer position for column-snap auto-scroll.
    // Duck-type rather than instanceof — jsdom/test environments may lack TouchEvent.
    const activator = event.activatorEvent;
    let pointerX = 0;
    if ('touches' in activator && (activator as TouchEvent).touches?.[0]) {
      pointerX = (activator as TouchEvent).touches[0].clientX;
    } else if ('clientX' in activator) {
      pointerX = (activator as MouseEvent).clientX;
    }
    initialPointerRef.current = { x: pointerX };
    snapAnchorXRef.current = pointerX;
    hasLeftEdgeZoneRef.current = true;

    // Record which column is currently visible
    if (scrollContainerRef.current) {
      currentColumnIndexRef.current = getCurrentColumnIndex(scrollContainerRef.current);
    }
  }, [columnItems]);

  // Column-snap auto-scroll: on mobile, when the pointer nears the viewport
  // edge during drag, scroll one column at a time. Two trigger mechanisms:
  // 1. Edge zones (primary): pointer within EDGE_THRESHOLD of viewport edge,
  //    but requires re-entry — pointer must leave the zone before it re-triggers.
  // 2. Distance from anchor (secondary): pointer moved >= SNAP_DISTANCE from
  //    the last snap position, enabling easy reverse-direction snapping.
  const handleDragMove = useCallback((event: DragMoveEvent) => {
    const container = scrollContainerRef.current;
    if (!container || !initialPointerRef.current || window.innerWidth >= SM_BREAKPOINT) return;

    const pointerX = initialPointerRef.current.x + event.delta.x;
    const inRightZone = pointerX > window.innerWidth - EDGE_THRESHOLD_PX;
    const inLeftZone = pointerX < EDGE_THRESHOLD_PX;

    // Track when the pointer leaves all edge zones (enables re-entry detection)
    if (!inRightZone && !inLeftZone) {
      hasLeftEdgeZoneRef.current = true;
    }

    const now = Date.now();
    if (now - lastSnapTimeRef.current < SNAP_COOLDOWN_MS) return;

    const maxIndex = sortedColumns.length - 1;
    const dx = pointerX - snapAnchorXRef.current;
    let targetIndex: number | null = null;

    // Primary: edge zone detection (requires re-entry after each snap)
    if (inRightZone && hasLeftEdgeZoneRef.current) {
      targetIndex = Math.min(maxIndex, currentColumnIndexRef.current + 1);
    } else if (inLeftZone && hasLeftEdgeZoneRef.current) {
      targetIndex = Math.max(0, currentColumnIndexRef.current - 1);
    }
    // Secondary: distance-based detection (enables reverse-direction snapping)
    else if (dx > SNAP_DISTANCE_PX) {
      targetIndex = Math.min(maxIndex, currentColumnIndexRef.current + 1);
    } else if (dx < -SNAP_DISTANCE_PX) {
      targetIndex = Math.max(0, currentColumnIndexRef.current - 1);
    }

    if (targetIndex !== null && targetIndex !== currentColumnIndexRef.current) {
      scrollToColumn(container, targetIndex);
      currentColumnIndexRef.current = targetIndex;
      lastSnapTimeRef.current = now;
      snapAnchorXRef.current = pointerX;
      hasLeftEdgeZoneRef.current = false;
    }
  }, [sortedColumns.length]);

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
      initialPointerRef.current = null;
      lastSnapTimeRef.current = 0;
      snapAnchorXRef.current = 0;
      hasLeftEdgeZoneRef.current = true;

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
    initialPointerRef.current = null;
    lastSnapTimeRef.current = 0;
    snapAnchorXRef.current = 0;
    hasLeftEdgeZoneRef.current = true;
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
      autoScroll={autoScrollConfig}
      onDragStart={handleDragStart}
      onDragMove={handleDragMove}
      onDragOver={handleDragOver}
      onDragEnd={handleDragEnd}
      onDragCancel={handleDragCancel}
    >
      <div ref={scrollContainerRef} className={cn('w-full overflow-x-auto sm:snap-none', !isDragging && 'snap-x snap-mandatory', className)}>
        <div className="flex min-h-full gap-4 p-4 sm:p-6" data-onboarding="board-columns">
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
      </div>

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
