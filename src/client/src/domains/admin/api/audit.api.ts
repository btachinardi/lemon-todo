import { apiClient } from '@/lib/api-client';
import type { AuditLogFilters, PagedAuditEntries } from '../types/audit.types';

/** API client for audit log endpoints. */
export const auditApi = {
  /** Searches audit log entries with optional filters (Admin+ required). */
  searchAuditLog(filters?: AuditLogFilters) {
    return apiClient.get<PagedAuditEntries>('/api/admin/audit', filters as Record<string, string | number | boolean | null | undefined>);
  },
};
