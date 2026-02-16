import { useMutation, useQueryClient } from '@tanstack/react-query';
import { adminApi } from '../api/admin.api';
import { adminKeys } from './use-admin-queries';
import { toastSuccess } from '@/lib/toast-helpers';

/** Assigns a role to a user and invalidates admin caches. */
export function useAssignRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      adminApi.assignRole(userId, roleName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess('Role assigned');
    },
  });
}

/** Removes a role from a user and invalidates admin caches. */
export function useRemoveRole() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      adminApi.removeRole(userId, roleName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess('Role removed');
    },
  });
}

/** Deactivates a user account and invalidates admin caches. */
export function useDeactivateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => adminApi.deactivateUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess('User deactivated');
    },
  });
}

/** Reactivates a deactivated user and invalidates admin caches. */
export function useReactivateUser() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => adminApi.reactivateUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess('User reactivated');
    },
  });
}
