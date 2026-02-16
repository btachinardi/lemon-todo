import { useTranslation } from 'react-i18next';
import { Badge } from '@/ui/badge';
import { TableCell, TableRow } from '@/ui/table';
import type { AuditEntry } from '../../types/audit.types';

const actionVariant: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  UserRegistered: 'default',
  UserLoggedIn: 'secondary',
  UserLoggedOut: 'secondary',
  RoleAssigned: 'default',
  RoleRemoved: 'outline',
  PiiRevealed: 'destructive',
  TaskCreated: 'default',
  TaskCompleted: 'secondary',
  TaskDeleted: 'destructive',
  UserDeactivated: 'destructive',
  UserReactivated: 'default',
};

function formatTimestamp(iso: string): string {
  const date = new Date(iso);
  return date.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  });
}

interface AuditLogRowProps {
  entry: AuditEntry;
}

/** Single row in the audit log table. */
export function AuditLogRow({ entry }: AuditLogRowProps) {
  const { t } = useTranslation();

  return (
    <TableRow>
      <TableCell className="text-xs text-muted-foreground whitespace-nowrap">
        {formatTimestamp(entry.timestamp)}
      </TableCell>
      <TableCell>
        <Badge variant={actionVariant[entry.action] ?? 'outline'} className="text-xs">
          {t(`admin.audit.actions.${entry.action}`)}
        </Badge>
      </TableCell>
      <TableCell className="text-xs">{entry.resourceType}</TableCell>
      <TableCell className="font-mono text-xs">
        {entry.resourceId ? `${entry.resourceId.slice(0, 8)}...` : '-'}
      </TableCell>
      <TableCell className="font-mono text-xs">
        {entry.actorId ? `${entry.actorId.slice(0, 8)}...` : t('admin.audit.system')}
      </TableCell>
      <TableCell className="max-w-xs truncate text-xs text-muted-foreground">
        {entry.details ?? '-'}
      </TableCell>
    </TableRow>
  );
}
