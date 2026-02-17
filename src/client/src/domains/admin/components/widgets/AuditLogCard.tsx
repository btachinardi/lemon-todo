import { useTranslation } from 'react-i18next';
import { Badge } from '@/ui/badge';
import type { AuditEntry } from '../../types/audit.types';

const actionVariant: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  UserRegistered: 'default',
  UserLoggedIn: 'secondary',
  UserLoggedOut: 'secondary',
  RoleAssigned: 'default',
  RoleRemoved: 'outline',
  ProtectedDataRevealed: 'destructive',
  TaskCreated: 'default',
  TaskCompleted: 'secondary',
  TaskDeleted: 'destructive',
  UserDeactivated: 'destructive',
  UserReactivated: 'default',
  SensitiveNoteRevealed: 'destructive',
  ProtectedDataAccessed: 'destructive',
  OwnProfileRevealed: 'secondary',
};

function formatTimestamp(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString(undefined, {
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  });
}

interface AuditLogCardProps {
  entry: AuditEntry;
}

/** Mobile card layout for a single audit log entry. */
export function AuditLogCard({ entry }: AuditLogCardProps) {
  const { t } = useTranslation();

  return (
    <div className="rounded-lg border p-3 space-y-2">
      <div className="flex items-center justify-between gap-2">
        <Badge variant={actionVariant[entry.action] ?? 'outline'} className="text-sm">
          {t(`admin.audit.actions.${entry.action}`)}
        </Badge>
        <span className="shrink-0 text-xs text-muted-foreground">
          {formatTimestamp(entry.timestamp)}
        </span>
      </div>
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <span>{entry.resourceType}</span>
        {entry.resourceId && (
          <>
            <span className="text-border">/</span>
            <span className="font-mono">{entry.resourceId.slice(0, 8)}...</span>
          </>
        )}
      </div>
      {entry.details && (
        <p className="text-sm text-muted-foreground/80 line-clamp-2">{entry.details}</p>
      )}
    </div>
  );
}
