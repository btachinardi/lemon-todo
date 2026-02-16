import { useQuery } from '@tanstack/react-query';
import { adminApi } from '../api/admin.api';

/** Query key factory for admin queries. */
export const adminKeys = {
  all: ['admin'] as const,
  users: (params?: { search?: string; role?: string; page?: number }) =>
    [...adminKeys.all, 'users', params] as const,
  user: (id: string) => [...adminKeys.all, 'user', id] as const,
};

/** Fetches paginated admin user list with optional filters. */
export function useAdminUsers(params?: { search?: string; role?: string; page?: number }) {
  return useQuery({
    queryKey: adminKeys.users(params),
    queryFn: () => adminApi.listUsers(params),
  });
}

/** Fetches a single admin user by ID. */
export function useAdminUser(id: string) {
  return useQuery({
    queryKey: adminKeys.user(id),
    queryFn: () => adminApi.getUser(id),
    enabled: !!id,
  });
}
