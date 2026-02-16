import type { Task } from '../types/task.types';
import { TaskStatus } from '../types/task.types';
import { GroupBy } from '../types/grouping.types';
import type { TaskGroup } from '../types/grouping.types';

/**
 * Groups tasks by a time-based key derived from `createdAt`.
 * Returns groups in reverse chronological order (most recent first).
 *
 * When `splitCompleted` is true, Done tasks are moved to `completedTasks`
 * within each group instead of appearing in the main `tasks` array.
 *
 * @returns Array of task groups with computed labels, ordered newest first
 */
export function groupTasksByDate(
  tasks: Task[],
  groupBy: GroupBy,
  splitCompleted: boolean,
): TaskGroup[] {
  if (tasks.length === 0) return [];

  if (groupBy === GroupBy.None) {
    return [buildFlatGroup(tasks, splitCompleted)];
  }

  const buckets = new Map<string, Task[]>();
  const keyOrder: string[] = [];

  for (const task of tasks) {
    const key = extractKey(task.createdAt, groupBy);
    const bucket = buckets.get(key);
    if (bucket) {
      bucket.push(task);
    } else {
      buckets.set(key, [task]);
      keyOrder.push(key);
    }
  }

  // Sort keys reverse-chronologically (lexicographic descending works for ISO keys)
  keyOrder.sort((a, b) => b.localeCompare(a));

  return keyOrder.map((key) => {
    const groupTasks = buckets.get(key)!;
    const label = formatLabel(key, groupBy);
    return partition(key, label, groupTasks, splitCompleted);
  });
}

/** Wraps all tasks into a single ungrouped TaskGroup. */
function buildFlatGroup(tasks: Task[], splitCompleted: boolean): TaskGroup {
  return partition('all', 'All Tasks', tasks, splitCompleted);
}

/** Splits tasks into active and completed arrays based on status. */
function partition(
  key: string,
  label: string,
  tasks: Task[],
  splitCompleted: boolean,
): TaskGroup {
  if (!splitCompleted) {
    return { key, label, tasks, completedTasks: [] };
  }

  const active: Task[] = [];
  const done: Task[] = [];
  for (const task of tasks) {
    if (task.status === TaskStatus.Done) {
      done.push(task);
    } else {
      active.push(task);
    }
  }
  return { key, label, tasks: active, completedTasks: done };
}

/** Converts an ISO timestamp to a grouping key (YYYY-MM-DD, YYYY-Www, or YYYY-MM). */
function extractKey(isoDate: string, groupBy: GroupBy): string {
  const date = new Date(isoDate);

  switch (groupBy) {
    case GroupBy.Day:
      return formatISODate(date);
    case GroupBy.Week:
      return formatISOWeekKey(date);
    case GroupBy.Month:
      return `${date.getUTCFullYear()}-${String(date.getUTCMonth() + 1).padStart(2, '0')}`;
    default:
      return 'all';
  }
}

/** Converts a grouping key to a human-readable label for display. */
function formatLabel(key: string, groupBy: GroupBy): string {
  switch (groupBy) {
    case GroupBy.Day: {
      const date = new Date(key + 'T00:00:00Z');
      return date.toLocaleDateString('en-US', {
        weekday: 'short',
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        timeZone: 'UTC',
      });
    }
    case GroupBy.Week: {
      // key = "2026-W03", parse the Monday of that week
      const monday = mondayFromWeekKey(key);
      const formatted = monday.toLocaleDateString('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        timeZone: 'UTC',
      });
      return `Week of ${formatted}`;
    }
    case GroupBy.Month: {
      const [year, month] = key.split('-');
      const date = new Date(Date.UTC(Number(year), Number(month) - 1, 1));
      return date.toLocaleDateString('en-US', {
        month: 'long',
        year: 'numeric',
        timeZone: 'UTC',
      });
    }
    default:
      return 'All Tasks';
  }
}

/** Returns "YYYY-MM-DD" in UTC. */
function formatISODate(date: Date): string {
  const y = date.getUTCFullYear();
  const m = String(date.getUTCMonth() + 1).padStart(2, '0');
  const d = String(date.getUTCDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

/** Calculates ISO week number and returns "YYYY-Www" key. Monday is day 1. */
function formatISOWeekKey(date: Date): string {
  const monday = getMonday(date);
  const year = monday.getUTCFullYear();
  const jan1 = new Date(Date.UTC(year, 0, 1));
  const jan1Day = jan1.getUTCDay() || 7; // ISO: Mon=1..Sun=7
  // Thursday of the same ISO week as jan1
  const daysSinceEpoch = Math.floor((monday.getTime() - jan1.getTime()) / 86_400_000);
  const weekNum = Math.floor((daysSinceEpoch + jan1Day - 1) / 7) + 1;
  return `${year}-W${String(weekNum).padStart(2, '0')}`;
}

/** Returns the Monday (UTC) of the ISO week containing `date`. */
function getMonday(date: Date): Date {
  const d = new Date(date);
  const day = d.getUTCDay() || 7; // Sun=0→7, Mon=1..Sat=6
  d.setUTCDate(d.getUTCDate() - (day - 1));
  d.setUTCHours(0, 0, 0, 0);
  return d;
}

/** Parses "YYYY-Www" ISO week key back to the Monday Date (UTC). */
function mondayFromWeekKey(key: string): Date {
  const [yearStr, weekStr] = key.split('-W');
  const year = Number(yearStr);
  const week = Number(weekStr);
  // Jan 4 is always in ISO week 1
  const jan4 = new Date(Date.UTC(year, 0, 4));
  const monday = getMonday(jan4);
  monday.setUTCDate(monday.getUTCDate() + (week - 1) * 7);
  return monday;
}
