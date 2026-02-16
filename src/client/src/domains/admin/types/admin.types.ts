/** User data returned by admin endpoints. Protected data is redacted by default. */
export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

/** Unredacted protected data returned by the reveal endpoint. */
export interface RevealedProtectedData {
  email: string;
  displayName: string;
}

/** Justification reasons for protected data reveal break-the-glass action. */
export const PROTECTED_DATA_REVEAL_REASONS = [
  'SupportTicket',
  'LegalRequest',
  'AccountRecovery',
  'SecurityInvestigation',
  'DataSubjectRequest',
  'ComplianceAudit',
  'Other',
] as const;

export type ProtectedDataRevealReason = (typeof PROTECTED_DATA_REVEAL_REASONS)[number];

/** Request body for the protected data reveal endpoint with break-the-glass controls. */
export interface RevealProtectedDataRequest {
  /** Justification category for the reveal action. */
  reason: ProtectedDataRevealReason;
  /** Required when reason is 'Other'. Describes the specific justification not covered by standard reasons. */
  reasonDetails?: string;
  /** Optional notes for auditors explaining the context or urgency of the reveal. */
  comments?: string;
  /** Current user's password for re-authentication and authorization verification. */
  password: string;
}

/** Paginated result from the admin users endpoint. */
export interface PagedAdminUsers {
  /** Users on the current page. Protected data is redacted unless explicitly revealed. */
  items: AdminUser[];
  /** Total number of users matching the filter across all pages. */
  totalCount: number;
  /** Current page number (1-indexed). */
  page: number;
  /** Number of items per page. */
  pageSize: number;
  /** Total number of pages available. */
  totalPages: number;
  /** True if there are more pages after the current one. */
  hasNextPage: boolean;
  /** True if there are pages before the current one. */
  hasPreviousPage: boolean;
}
