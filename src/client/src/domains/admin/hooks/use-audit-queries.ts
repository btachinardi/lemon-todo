import { useQuery } from '@tanstack/react-query';
import { auditApi } from '../api/audit.api';
import type { AuditLogFilters } from '../types/audit.types';

/** Query key factory for audit log queries. */
export const auditKeys = {
  all: ['audit'] as const,
  search: (filters?: AuditLogFilters) => [...auditKeys.all, 'search', filters] as const,
};

/** Fetches paginated audit log entries with optional filters. */
export function useAuditLog(filters?: AuditLogFilters) {
  return useQuery({
    queryKey: auditKeys.search(filters),
    queryFn: () => auditApi.searchAuditLog(filters),
    staleTime: 1000 * 5,
  });
}
