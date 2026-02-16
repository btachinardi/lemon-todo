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
  reason: ProtectedDataRevealReason;
  reasonDetails?: string;
  comments?: string;
  password: string;
}

/** Paginated result from the admin users endpoint. */
export interface PagedAdminUsers {
  items: AdminUser[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}
