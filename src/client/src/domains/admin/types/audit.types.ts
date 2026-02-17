import type { components } from '../../../api/schema';

/** Actions recorded in the audit trail, derived from the backend AuditAction enum via OpenAPI schema. */
export type AuditAction = components['schemas']['AuditAction'];

/** A single audit trail entry. */
export type AuditEntry = components['schemas']['AuditEntryDto'];

/** Paginated result from the audit log endpoint. */
export interface PagedAuditEntries {
  items: AuditEntry[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/** Filters for the audit log search. */
export interface AuditLogFilters {
  dateFrom?: string;
  dateTo?: string;
  action?: AuditAction;
  actorId?: string;
  resourceType?: string;
  page?: number;
  pageSize?: number;
}
