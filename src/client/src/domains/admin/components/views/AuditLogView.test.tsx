import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, type Mock } from 'vitest';
import { AuditLogView } from './AuditLogView';
import type { PagedAuditEntries, AuditEntry } from '../../types/audit.types';

// Mock i18next — return key (with interpolated params) for deterministic assertions
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, params?: Record<string, unknown>) => {
      if (params) return `${key}:${JSON.stringify(params)}`;
      return key;
    },
  }),
}));

// Mock the audit query hook
vi.mock('../../hooks/use-audit-queries', () => ({
  useAuditLog: vi.fn(),
}));

import { useAuditLog } from '../../hooks/use-audit-queries';

function makeEntry(overrides: Partial<AuditEntry> = {}): AuditEntry {
  return {
    id: crypto.randomUUID(),
    timestamp: '2026-02-15T10:30:00Z',
    actorId: '00000000-0000-0000-0000-000000000099',
    action: 'TaskCreated',
    resourceType: 'Task',
    resourceId: '00000000-0000-0000-0000-000000000042',
    details: 'Created task',
    ipAddress: '127.0.0.1',
    ...overrides,
  };
}

function makePagedResponse(overrides: Partial<PagedAuditEntries> = {}): PagedAuditEntries {
  return {
    items: [makeEntry()],
    totalCount: 1,
    page: 1,
    pageSize: 20,
    totalPages: 1,
    hasNextPage: false,
    hasPreviousPage: false,
    ...overrides,
  };
}

function mockUseAuditLog(data: PagedAuditEntries | undefined, isLoading = false) {
  (useAuditLog as Mock).mockReturnValue({ data, isLoading });
}

describe('AuditLogView', () => {
  describe('pagination', () => {
    it('should show pagination footer when data has a single page', () => {
      mockUseAuditLog(makePagedResponse({ totalCount: 5, totalPages: 1 }));
      render(<AuditLogView />);

      expect(screen.getByText(/common\.page/)).toBeInTheDocument();
    });

    it('should show pagination footer when data has multiple pages', () => {
      mockUseAuditLog(
        makePagedResponse({ totalCount: 45, totalPages: 3, hasNextPage: true }),
      );
      render(<AuditLogView />);

      expect(screen.getByText(/common\.page/)).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeInTheDocument();
    });

    it('should disable both buttons on single page', () => {
      mockUseAuditLog(makePagedResponse({ totalPages: 1 }));
      render(<AuditLogView />);

      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeDisabled();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeDisabled();
    });

    it('should disable Previous and enable Next on first page of many', () => {
      mockUseAuditLog(
        makePagedResponse({ totalCount: 45, totalPages: 3, hasNextPage: true, hasPreviousPage: false }),
      );
      render(<AuditLogView />);

      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeDisabled();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeEnabled();
    });

    it('should disable Next and enable Previous on last page', () => {
      mockUseAuditLog(
        makePagedResponse({ page: 3, totalCount: 45, totalPages: 3, hasNextPage: false, hasPreviousPage: true }),
      );
      render(<AuditLogView />);

      expect(screen.getByRole('button', { name: /common\.previous/ })).toBeEnabled();
      expect(screen.getByRole('button', { name: /common\.next/ })).toBeDisabled();
    });

    it('should navigate to next page when Next is clicked', async () => {
      const user = userEvent.setup();
      mockUseAuditLog(
        makePagedResponse({ totalCount: 45, totalPages: 3, hasNextPage: true }),
      );
      render(<AuditLogView />);

      await user.click(screen.getByRole('button', { name: /common\.next/ }));

      // The hook should be re-called with page: 2
      const lastCall = (useAuditLog as Mock).mock.calls.at(-1)?.[0];
      expect(lastCall).toMatchObject({ page: 2 });
    });

    it('should not show pagination footer when there are no entries', () => {
      mockUseAuditLog(makePagedResponse({ items: [], totalCount: 0, totalPages: 0 }));
      render(<AuditLogView />);

      expect(screen.queryByText(/common\.page/)).not.toBeInTheDocument();
    });
  });
});
