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
  | 'ProtectedDataAccessed'
  | 'SensitiveNoteRevealed'
  | 'OwnProfileRevealed';

/** A single audit trail entry. */
export interface AuditEntry {
  id: string;
  timestamp: string;
  /** ID of the user who performed the action. Null for system-initiated actions (e.g., automated cleanup). */
  actorId: string | null;
  action: AuditAction;
  resourceType: string;
  /** ID of the affected resource. Null for non-resource actions (e.g., UserLoggedIn, UserLoggedOut). */
  resourceId: string | null;
  /** Optional JSON payload with action-specific metadata (e.g., reveal reason, changed fields). */
  details: string | null;
  /** IP address of the client. Null for system-initiated actions or when IP cannot be determined. */
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
