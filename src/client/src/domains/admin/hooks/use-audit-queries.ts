import { useQuery } from '@tanstack/react-query';
import { auditApi } from '../api/audit.api';
import type { AuditLogFilters } from '../types/audit.types';

/** Query key factory for audit log queries. */
export const auditKeys = {
  all: ['audit'] as const,
  search: (filters?: AuditLogFilters) => [...auditKeys.all, 'search', filters] as const,
};

/**
 * Query for paginated audit log entries with optional filters.
 * Results are cached for 5 seconds to reduce server load while allowing near-real-time
 * updates for compliance monitoring. Audit data is immutable, so stale data is safe.
 * @returns Query result with PagedAuditEntries data, including loading and error states.
 */
export function useAuditLog(filters?: AuditLogFilters) {
  return useQuery({
    queryKey: auditKeys.search(filters),
    queryFn: () => auditApi.searchAuditLog(filters),
    staleTime: 1000 * 5, // 5 seconds: balance between server load and freshness for compliance UX
  });
}
