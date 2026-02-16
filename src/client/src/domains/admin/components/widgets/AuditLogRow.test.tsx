import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { AuditLogRow } from './AuditLogRow';
import type { AuditEntry } from '../../types/audit.types';

// Mock i18next — return key as value for deterministic assertions
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, params?: Record<string, unknown>) => {
      if (params) return `${key}:${JSON.stringify(params)}`;
      return key;
    },
  }),
}));

function makeEntry(overrides: Partial<AuditEntry> = {}): AuditEntry {
  return {
    id: '00000000-0000-0000-0000-000000000001',
    timestamp: '2026-01-15T10:30:00Z',
    actorId: '00000000-0000-0000-0000-000000000099',
    action: 'TaskCreated',
    resourceType: 'Task',
    resourceId: '00000000-0000-0000-0000-000000000042',
    details: 'Created task',
    ipAddress: '127.0.0.1',
    ...overrides,
  };
}

// AuditLogRow renders inside a <table> so we need a wrapper
function renderRow(entry: AuditEntry) {
  return render(
    <table>
      <tbody>
        <AuditLogRow entry={entry} />
      </tbody>
    </table>,
  );
}

describe('AuditLogRow', () => {
  it('should render action label from i18n translation key', () => {
    renderRow(makeEntry({ action: 'TaskCreated' }));
    expect(screen.getByText('admin.audit.actions.TaskCreated')).toBeInTheDocument();
  });

  it('should render i18n key for each audit action', () => {
    const actions = [
      'UserRegistered',
      'UserLoggedIn',
      'UserLoggedOut',
      'RoleAssigned',
      'RoleRemoved',
      'PiiRevealed',
      'TaskCreated',
      'TaskCompleted',
      'TaskDeleted',
      'UserDeactivated',
      'UserReactivated',
    ] as const;

    for (const action of actions) {
      const { unmount } = renderRow(makeEntry({ action }));
      expect(screen.getByText(`admin.audit.actions.${action}`)).toBeInTheDocument();
      unmount();
    }
  });

  it('should display system label for entries without an actor', () => {
    renderRow(makeEntry({ actorId: null }));
    expect(screen.getByText('admin.audit.system')).toBeInTheDocument();
  });
});
