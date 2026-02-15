import type { TaskStatus } from './task.types';

/**
 * A vertical lane on the kanban board. Each column maps to exactly one
 * {@link TaskStatus} via `targetStatus`, so moving a card between columns
 * also triggers a status transition on the backend.
 */
export interface Column {
  id: string;
  name: string;
  /** The task status that cards in this column represent. */
  targetStatus: TaskStatus;
  /** Zero-based display order (left to right). */
  position: number;
  /** WIP limit; `null` means unlimited. */
  maxTasks: number | null;
}

/**
 * Placement of a task on a board. The board aggregate owns these --
 * they are not stored on the task itself.
 */
export interface TaskCard {
  taskId: string;
  columnId: string;
  /** Zero-based display order within the column (top to bottom). */
  position: number;
}

/**
 * Read-model projection of a board aggregate. A board organizes tasks
 * into columns via {@link TaskCard} entries.
 *
 * The app uses a single default board in CP1 (single-user mode).
 */
export interface Board {
  id: string;
  name: string;
  columns: Column[];
  /** Card placements; may be absent on lightweight board queries. */
  cards?: TaskCard[];
  createdAt: string;
}
