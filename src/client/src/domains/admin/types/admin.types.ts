/** User data returned by admin endpoints. PII is redacted by default. */
export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
  isActive: boolean;
  createdAt: string;
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
