import { useQuery } from '@tanstack/react-query';
import { boardsApi } from '../api/boards.api';

/**
 * TanStack Query key factory for board queries.
 * @see {@link taskKeys} -- mutation hooks invalidate both key trees
 * because board cards change when tasks mutate.
 */
export const boardKeys = {
  all: ['boards'] as const,
  details: () => [...boardKeys.all, 'detail'] as const,
  detail: (id: string) => [...boardKeys.details(), id] as const,
  default: () => [...boardKeys.all, 'default'] as const,
};

/** Fetches the single default board (CP1 single-user mode). */
export function useDefaultBoardQuery() {
  return useQuery({
    queryKey: boardKeys.default(),
    queryFn: () => boardsApi.getDefault(),
  });
}

/**
 * Fetches a board by ID. Disabled when `id` is falsy.
 * @param id - Unique board identifier
 */
export function useBoardQuery(id: string) {
  return useQuery({
    queryKey: boardKeys.detail(id),
    queryFn: () => boardsApi.getById(id),
    enabled: !!id,
  });
}
