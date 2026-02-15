import type { Task } from './task.types';

/** Grouping strategy for the list view. */
export const GroupBy = {
  /** No grouping — flat list (default). */
  None: 'none',
  /** Group by calendar day of `createdAt`. */
  Day: 'day',
  /** Group by ISO week of `createdAt`. */
  Week: 'week',
  /** Group by calendar month of `createdAt`. */
  Month: 'month',
} as const;

export type GroupBy = (typeof GroupBy)[keyof typeof GroupBy];

/** A cluster of tasks sharing the same time-based group key. */
export interface TaskGroup {
  /** Opaque key for React keying (e.g. "2026-01-15", "2026-W03", "2026-01"). */
  key: string;
  /** Human-readable label (e.g. "Wed, Jan 15, 2026", "Week of Jan 13, 2026"). */
  label: string;
  /** Non-completed tasks in this group. */
  tasks: Task[];
  /** Completed (Done) tasks in this group. Empty when `splitCompleted` is false. */
  completedTasks: Task[];
}
