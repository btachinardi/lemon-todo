import { useQuery } from '@tanstack/react-query';
import { boardsApi } from '../api/boards.api';

export const boardKeys = {
  all: ['boards'] as const,
  details: () => [...boardKeys.all, 'detail'] as const,
  detail: (id: string) => [...boardKeys.details(), id] as const,
  default: () => [...boardKeys.all, 'default'] as const,
};

export function useDefaultBoardQuery() {
  return useQuery({
    queryKey: boardKeys.default(),
    queryFn: () => boardsApi.getDefault(),
  });
}

export function useBoardQuery(id: string) {
  return useQuery({
    queryKey: boardKeys.detail(id),
    queryFn: () => boardsApi.getById(id),
    enabled: !!id,
  });
}
