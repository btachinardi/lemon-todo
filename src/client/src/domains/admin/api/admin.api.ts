import { apiClient } from '@/lib/api-client';
import type { AdminUser, PagedAdminUsers, RevealedPii } from '../types/admin.types';

/** API client for admin user management endpoints. */
export const adminApi = {
  /** Lists users with optional search/role filter (Admin+ required). */
  listUsers(params?: { search?: string; role?: string; page?: number; pageSize?: number }) {
    return apiClient.get<PagedAdminUsers>('/api/admin/users', params);
  },

  /** Gets a single user by ID (Admin+ required). */
  getUser(id: string) {
    return apiClient.get<AdminUser>(`/api/admin/users/${id}`);
  },

  /** Assigns a role to a user (SystemAdmin required). */
  assignRole(userId: string, roleName: string) {
    return apiClient.post<{ success: boolean }>(`/api/admin/users/${userId}/roles`, { roleName });
  },

  /** Removes a role from a user (SystemAdmin required). */
  removeRole(userId: string, roleName: string) {
    return apiClient.delete<{ success: boolean }>(`/api/admin/users/${userId}/roles/${roleName}`);
  },

  /** Deactivates a user account (SystemAdmin required). */
  deactivateUser(userId: string) {
    return apiClient.post<{ success: boolean }>(`/api/admin/users/${userId}/deactivate`);
  },

  /** Reactivates a deactivated user (SystemAdmin required). */
  reactivateUser(userId: string) {
    return apiClient.post<{ success: boolean }>(`/api/admin/users/${userId}/reactivate`);
  },

  /** Reveals a user's unredacted PII (SystemAdmin required). Logged in audit trail. */
  revealPii(userId: string) {
    return apiClient.post<RevealedPii>(`/api/admin/users/${userId}/reveal`);
  },
};
