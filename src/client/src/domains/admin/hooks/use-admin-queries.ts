import { useQuery } from '@tanstack/react-query';
import { adminApi } from '../api/admin.api';

/** Query key factory for admin queries. */
export const adminKeys = {
  all: ['admin'] as const,
  users: (params?: { search?: string; role?: string; page?: number; pageSize?: number }) =>
    [...adminKeys.all, 'users', params] as const,
  user: (id: string) => [...adminKeys.all, 'user', id] as const,
};

/**
 * Query for paginated admin user list with optional filters.
 * Results are cached by TanStack Query and invalidated on role/status mutations.
 * Requires Admin+ authorization.
 * @returns Query result with PagedAdminUsers data, including loading and error states.
 */
export function useAdminUsers(params?: { search?: string; role?: string; page?: number; pageSize?: number }) {
  return useQuery({
    queryKey: adminKeys.users(params),
    queryFn: () => adminApi.listUsers(params),
  });
}

/**
 * Query for a single admin user by ID with redacted protected data.
 * Query is disabled if ID is empty. Requires Admin+ authorization.
 * @returns Query result with AdminUser data (protected fields redacted), including loading and error states.
 */
export function useAdminUser(id: string) {
  return useQuery({
    queryKey: adminKeys.user(id),
    queryFn: () => adminApi.getUser(id),
    enabled: !!id,
  });
}
