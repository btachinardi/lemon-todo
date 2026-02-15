/**
 * Task priority levels, ordered from lowest to highest urgency.
 * Values match the backend enum and are used in API serialization.
 */
export const Priority = {
  /** No priority assigned (default for quick-added tasks). */
  None: 'None',
  /** Nice to have; no deadline pressure. */
  Low: 'Low',
  /** Normal work item. */
  Medium: 'Medium',
  /** Needs attention soon. */
  High: 'High',
  /** Blocking other work; handle immediately. */
  Critical: 'Critical',
} as const;

/** Union of valid priority levels. Use the companion `Priority` const for values. */
export type Priority = (typeof Priority)[keyof typeof Priority];

/**
 * Task lifecycle statuses. Transitions are enforced by the backend domain:
 * - `Todo` -> `InProgress` -> `Done` (and reverse via uncomplete).
 *
 * Board columns map to these statuses via {@link Column.targetStatus}.
 */
export const TaskStatus = {
  /** Initial state for newly created tasks. */
  Todo: 'Todo',
  /** Task is actively being worked on. */
  InProgress: 'InProgress',
  /** Task is finished; sets `completedAt` timestamp. */
  Done: 'Done',
} as const;

/** Union of valid task lifecycle statuses. Use the companion `TaskStatus` const for values. */
export type TaskStatus = (typeof TaskStatus)[keyof typeof TaskStatus];

/**
 * Read-model projection of a task aggregate from the API.
 * All date fields are ISO 8601 strings (UTC) as returned by the backend.
 */
export interface Task {
  id: string;
  title: string;
  description: string | null;
  priority: Priority;
  status: TaskStatus;
  /** ISO 8601 date string (date only, no time component). */
  dueDate: string | null;
  tags: string[];
  /** Visibility flag -- archived tasks are hidden from default views. */
  isArchived: boolean;
  /** Soft-delete flag -- deleted tasks are excluded from all queries. */
  isDeleted: boolean;
  /** Set when status transitions to `Done`; cleared on uncomplete. */
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
}
