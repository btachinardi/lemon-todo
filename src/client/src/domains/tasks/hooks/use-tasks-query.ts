import { useQuery } from '@tanstack/react-query';
import { tasksApi } from '../api/tasks.api';
import type { ListTasksParams } from '../types/api.types';

/**
 * TanStack Query key factory for task queries.
 * Follows the hierarchical key pattern so invalidating `taskKeys.all`
 * clears every task-related cache entry.
 */
export const taskKeys = {
  all: ['tasks'] as const,
  lists: () => [...taskKeys.all, 'list'] as const,
  list: (params?: ListTasksParams) => [...taskKeys.lists(), params] as const,
  details: () => [...taskKeys.all, 'detail'] as const,
  detail: (id: string) => [...taskKeys.details(), id] as const,
};

/** Fetches a paginated, filterable list of tasks. */
export function useTasksQuery(params?: ListTasksParams) {
  return useQuery({
    queryKey: taskKeys.list(params),
    queryFn: () => tasksApi.list(params),
  });
}

/** Fetches a single task by ID. Disabled when `id` is falsy. */
export function useTaskQuery(id: string) {
  return useQuery({
    queryKey: taskKeys.detail(id),
    queryFn: () => tasksApi.getById(id),
    enabled: !!id,
  });
}
