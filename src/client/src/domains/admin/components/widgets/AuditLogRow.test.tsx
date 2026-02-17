import { render, screen } from '@testing-library/react';
import { describe, it, expect, vi } from 'vitest';
import { AuditLogRow } from './AuditLogRow';
import type { AuditAction, AuditEntry } from '../../types/audit.types';

// Mock i18next — return key as value for deterministic assertions
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, params?: Record<string, unknown>) => {
      if (params) return `${key}:${JSON.stringify(params)}`;
      return key;
    },
  }),
}));

/** All AuditAction values — must match the backend AuditAction enum exactly. */
const ALL_AUDIT_ACTIONS: AuditAction[] = [
  'UserRegistered',
  'UserLoggedIn',
  'UserLoggedOut',
  'RoleAssigned',
  'RoleRemoved',
  'ProtectedDataRevealed',
  'ProtectedDataAccessed',
  'TaskCreated',
  'TaskCompleted',
  'TaskDeleted',
  'UserDeactivated',
  'UserReactivated',
  'SensitiveNoteRevealed',
  'OwnProfileRevealed',
];

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

  it('should render i18n key for every audit action', () => {
    for (const action of ALL_AUDIT_ACTIONS) {
      const { unmount } = renderRow(makeEntry({ action }));
      expect(screen.getByText(`admin.audit.actions.${action}`)).toBeInTheDocument();
      unmount();
    }
  });

  it('should have an explicit badge variant for every audit action (not the outline fallback)', () => {
    // The actionVariant map falls back to 'outline' for unknown actions via ?? operator.
    // Every action should have an explicit mapping — 'outline' is only valid when intentional (e.g. RoleRemoved).
    // We check by rendering each action and verifying the badge has a data-variant that is NOT the default fallback
    // for actions that should NOT be outline.
    const intentionallyOutline = new Set<AuditAction>(['RoleRemoved']);

    for (const action of ALL_AUDIT_ACTIONS) {
      const { unmount } = renderRow(makeEntry({ action }));
      const badge = screen.getByText(`admin.audit.actions.${action}`);
      const variant = badge.getAttribute('data-variant');

      if (!intentionallyOutline.has(action)) {
        expect(variant, `Action '${action}' should have an explicit variant, not fallback 'outline'`).not.toBe('outline');
      }
      unmount();
    }
  });

  it('should display system label for entries without an actor', () => {
    renderRow(makeEntry({ actorId: null }));
    expect(screen.getByText('admin.audit.system')).toBeInTheDocument();
  });
});
