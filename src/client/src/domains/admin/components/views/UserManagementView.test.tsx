import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, type Mock } from 'vitest';
import { UserManagementView } from './UserManagementView';
import type { PagedAdminUsers, AdminUser } from '../../types/admin.types';

// Mock i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, params?: Record<string, unknown>) => {
      if (params) return `${key}:${JSON.stringify(params)}`;
      return key;
    },
  }),
}));

// Mock admin query hook
vi.mock('../../hooks/use-admin-queries', () => ({
  useAdminUsers: vi.fn(),
  useAdminUser: vi.fn().mockReturnValue({ data: undefined }),
}));

// Mock admin mutation hooks
vi.mock('../../hooks/use-admin-mutations', () => ({
  useAssignRole: vi.fn().mockReturnValue({ mutate: vi.fn(), isPending: false }),
  useRemoveRole: vi.fn().mockReturnValue({ mutate: vi.fn() }),
  useDeactivateUser: vi.fn().mockReturnValue({ mutate: vi.fn() }),
  useReactivateUser: vi.fn().mockReturnValue({ mutate: vi.fn() }),
  useRevealProtectedData: vi.fn().mockReturnValue({ mutate: vi.fn(), isPending: false }),
}));

// Mock auth store — default to non-system-admin
vi.mock('@/domains/auth/stores/use-auth-store', () => ({
  useAuthStore: vi.fn((selector: (s: { user: { roles: string[] } | null }) => unknown) =>
    selector({ user: { roles: ['Admin'] } }),
  ),
}));

import { useAdminUsers } from '../../hooks/use-admin-queries';

function makeUser(overrides: Partial<AdminUser> = {}): AdminUser {
  return {
    id: crypto.randomUUID(),
    email: 'user@example.com',
    displayName: 'Test User',
    roles: ['User'],
    isActive: true,
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

function makePagedResponse(overrides: Partial<PagedAdminUsers> = {}): PagedAdminUsers {
  return {
    items: [makeUser()],
    totalCount: 1,
    page: 1,
    pageSize: 10,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
    ...overrides,
  };
}

function mockUseAdminUsers(data: PagedAdminUsers | undefined, isLoading = false) {
  (useAdminUsers as Mock).mockReturnValue({ data, isLoading });
}

describe('UserManagementView', () => {
  describe('default page size', () => {
    it('should request pageSize of 10 on initial render', () => {
      mockUseAdminUsers(makePagedResponse());
      render(<UserManagementView />);

      const firstCall = (useAdminUsers as Mock).mock.calls[0]?.[0];
      expect(firstCall).toMatchObject({ pageSize: 10 });
    });
  });

  describe('pagination', () => {
    it('should show pagination footer when data has a single page', () => {
      mockUseAdminUsers(makePagedResponse({ totalCount: 3, totalPages: 1 }));
      render(<UserManagementView />);

      expect(screen.getByText(/common\.page/)).toBeInTheDocument();
    });

    it('should show pagination footer when data has multiple pages', () => {
      mockUseAdminUsers(
        makePagedResponse({ totalCount: 45, totalPages: 3, hasNextPage: true }),
      );
      render(<UserManagementView />);

      expect(screen.getByText(/common\.page/)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeInTheDocument();
    });

    it('should disable both buttons on single page', () => {
      mockUseAdminUsers(makePagedResponse({ totalPages: 1 }));
      render(<UserManagementView />);

      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeDisabled();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeDisabled();
    });

    it('should disable Previous and enable Next on first page of many', () => {
      mockUseAdminUsers(
        makePagedResponse({ totalCount: 45, totalPages: 3, hasNextPage: true, hasPreviousPage: false }),
      );
      render(<UserManagementView />);

      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeDisabled();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeEnabled();
    });

    it('should disable Next and enable Previous on last page', () => {
      mockUseAdminUsers(
        makePagedResponse({ page: 3, totalCount: 45, totalPages: 3, hasNextPage: false, hasPreviousPage: true }),
      );
      render(<UserManagementView />);

      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeEnabled();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeDisabled();
    });

    it('should navigate to next page when Next is clicked', async () => {
      const user = userEvent.setup();
      mockUseAdminUsers(
        makePagedResponse({ totalCount: 45, totalPages: 3, hasNextPage: true }),
      );
      render(<UserManagementView />);

      await user.click(screen.getByRole('button', { name: /common\.next/ }));

      // The hook should be re-called with page: 2
      const lastCall = (useAdminUsers as Mock).mock.calls.at(-1)?.[0];
      expect(lastCall).toMatchObject({ page: 2 });
    });

    it('should not show pagination footer when there are no users', () => {
      mockUseAdminUsers(makePagedResponse({ items: [], totalCount: 0, totalPages: 0 }));
      render(<UserManagementView />);

      expect(screen.queryByText(/common\.page/)).not.toBeInTheDocument();
    });
  });
});
