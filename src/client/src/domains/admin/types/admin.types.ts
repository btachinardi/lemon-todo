/** User data returned by admin endpoints. PII is redacted by default. */
export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
}

/** Unredacted PII returned by the reveal endpoint. */
export interface RevealedPii {
  email: string;
  displayName: string;
}

/** Justification reasons for PII reveal break-the-glass action. */
export const PII_REVEAL_REASONS = [
  'SupportTicket',
  'LegalRequest',
  'AccountRecovery',
  'SecurityInvestigation',
  'DataSubjectRequest',
  'ComplianceAudit',
  'Other',
] as const;

export type PiiRevealReason = (typeof PII_REVEAL_REASONS)[number];

/** Request body for the PII reveal endpoint with break-the-glass controls. */
export interface RevealPiiRequest {
  reason: PiiRevealReason;
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
