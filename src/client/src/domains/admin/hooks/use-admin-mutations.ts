import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { adminApi } from '../api/admin.api';
import { adminKeys } from './use-admin-queries';
import { toastSuccess } from '@/lib/toast-helpers';
import type { RevealProtectedDataRequest } from '../types/admin.types';

/** Assigns a role to a user and invalidates admin caches. */
export function useAssignRole() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      adminApi.assignRole(userId, roleName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess(t('admin.toasts.roleAssigned'));
    },
  });
}

/** Removes a role from a user and invalidates admin caches. */
export function useRemoveRole() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ userId, roleName }: { userId: string; roleName: string }) =>
      adminApi.removeRole(userId, roleName),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess(t('admin.toasts.roleRemoved'));
    },
  });
}

/** Deactivates a user account and invalidates admin caches. */
export function useDeactivateUser() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => adminApi.deactivateUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess(t('admin.toasts.userDeactivated'));
    },
  });
}

/** Reactivates a deactivated user and invalidates admin caches. */
export function useReactivateUser() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => adminApi.reactivateUser(userId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminKeys.all });
      toastSuccess(t('admin.toasts.userReactivated'));
    },
  });
}

/** Reveals a user's unredacted protected data with break-the-glass controls. Logged in audit trail. */
export function useRevealProtectedData() {
  return useMutation({
    mutationFn: ({ userId, request }: { userId: string; request: RevealProtectedDataRequest }) =>
      adminApi.revealProtectedData(userId, request),
  });
}
