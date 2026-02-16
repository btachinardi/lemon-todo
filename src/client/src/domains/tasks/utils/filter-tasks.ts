import type { Task } from '../types/task.types';

interface FilterCriteria {
  searchTerm?: string;
  filterPriority?: string;
  filterStatus?: string;
  filterTag?: string;
}

/**
 * Pure function that applies client-side filters to a task list.
 * Used by kanban board (which gets all tasks at once) to apply filters
 * without hitting the server. All criteria are combined with AND logic.
 *
 * Client-side filtering exists because the board loads all tasks upfront
 * for drag-and-drop positioning. This avoids extra API round-trips when
 * applying filters to the already-loaded set.
 *
 * @returns Filtered array of tasks matching all provided criteria
 */
export function filterTasks(tasks: Task[], criteria: FilterCriteria): Task[] {
  const { searchTerm, filterPriority, filterStatus, filterTag } = criteria;

  return tasks.filter((task) => {
    if (filterPriority && task.priority !== filterPriority) return false;
    if (filterStatus && task.status !== filterStatus) return false;
    if (filterTag && !task.tags.includes(filterTag)) return false;
    if (searchTerm) {
      const term = searchTerm.toLowerCase();
      const titleMatch = task.title.toLowerCase().includes(term);
      const descMatch = task.description?.toLowerCase().includes(term) ?? false;
      if (!titleMatch && !descMatch) return false;
    }
    return true;
  });
}

/** Returns true if any filter is actively set. */
export function hasActiveFilters(criteria: FilterCriteria): boolean {
  return !!(criteria.searchTerm || criteria.filterPriority || criteria.filterStatus || criteria.filterTag);
}
