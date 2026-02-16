/** Actions recorded in the audit trail. Must match backend AuditAction enum. */
export type AuditAction =
  | 'UserRegistered'
  | 'UserLoggedIn'
  | 'UserLoggedOut'
  | 'RoleAssigned'
  | 'RoleRemoved'
  | 'ProtectedDataRevealed'
  | 'TaskCreated'
  | 'TaskCompleted'
  | 'TaskDeleted'
  | 'UserDeactivated'
  | 'UserReactivated'
  | 'SensitiveNoteRevealed';

/** A single audit trail entry. */
export interface AuditEntry {
  id: string;
  timestamp: string;
  actorId: string | null;
  action: AuditAction;
  resourceType: string;
  resourceId: string | null;
  details: string | null;
  ipAddress: string | null;
}

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
