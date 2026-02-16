import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { adminApi } from '../api/admin.api';
import { adminKeys } from './use-admin-queries';
import { toastSuccess } from '@/lib/toast-helpers';

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

/** Reveals a user's unredacted PII. This action is logged in the audit trail. */
export function useRevealPii() {
  return useMutation({
    mutationFn: (userId: string) => adminApi.revealPii(userId),
  });
}
