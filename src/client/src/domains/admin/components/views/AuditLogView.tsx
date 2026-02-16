import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/ui/select';
import {
  Table,
  TableBody,
  TableHead,
  TableHeader,
  TableRow,
} from '@/ui/table';
import { ChevronLeftIcon, ChevronRightIcon } from 'lucide-react';
import { useAuditLog } from '../../hooks/use-audit-queries';
import { AuditLogRow } from '../widgets/AuditLogRow';
import type { AuditAction, AuditLogFilters } from '../../types/audit.types';

const AUDIT_ACTIONS: AuditAction[] = [
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
];

const RESOURCE_TYPES = ['User', 'Task'];

/** Audit log viewer with filters, table, and pagination. */
export function AuditLogView() {
  const { t } = useTranslation();
  const [filters, setFilters] = useState<AuditLogFilters>({
    page: 1,
    pageSize: 20,
  });

  const { data, isLoading } = useAuditLog(filters);

  const updateFilter = (updates: Partial<AuditLogFilters>) => {
    setFilters((prev) => ({ ...prev, ...updates, page: 1 }));
  };

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-wrap items-end gap-3">
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">{t('admin.audit.from')}</label>
          <Input
            type="date"
            className="w-40"
            value={filters.dateFrom ?? ''}
            onChange={(e) => updateFilter({ dateFrom: e.target.value || undefined })}
          />
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">{t('admin.audit.to')}</label>
          <Input
            type="date"
            className="w-40"
            value={filters.dateTo ?? ''}
            onChange={(e) => updateFilter({ dateTo: e.target.value || undefined })}
          />
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">{t('admin.audit.action')}</label>
          <Select
            value={filters.action ?? '__all__'}
            onValueChange={(v) => updateFilter({ action: v === '__all__' ? undefined : (v as AuditAction) })}
          >
            <SelectTrigger className="w-44">
              <SelectValue placeholder={t('admin.audit.allActions')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">{t('admin.audit.allActions')}</SelectItem>
              {AUDIT_ACTIONS.map((action) => (
                <SelectItem key={action} value={action}>
                  {t(`admin.audit.actions.${action}`)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">{t('admin.audit.resource')}</label>
          <Select
            value={filters.resourceType ?? '__all__'}
            onValueChange={(v) => updateFilter({ resourceType: v === '__all__' ? undefined : v })}
          >
            <SelectTrigger className="w-32">
              <SelectValue placeholder={t('admin.audit.allResources')} />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">{t('admin.audit.allResources')}</SelectItem>
              {RESOURCE_TYPES.map((rt) => (
                <SelectItem key={rt} value={rt}>
                  {rt}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {/* Table */}
      <div className="rounded-md border">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-44">{t('admin.audit.columnTimestamp')}</TableHead>
              <TableHead className="w-36">{t('admin.audit.columnAction')}</TableHead>
              <TableHead className="w-24">{t('admin.audit.columnResource')}</TableHead>
              <TableHead className="w-28">{t('admin.audit.columnResourceId')}</TableHead>
              <TableHead className="w-28">{t('admin.audit.columnActor')}</TableHead>
              <TableHead>{t('admin.audit.columnDetails')}</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <td colSpan={6} className="py-8 text-center text-muted-foreground">
                  {t('common.loading')}
                </td>
              </TableRow>
            ) : data && data.items.length > 0 ? (
              data.items.map((entry) => <AuditLogRow key={entry.id} entry={entry} />)
            ) : (
              <TableRow>
                <td colSpan={6} className="py-8 text-center text-muted-foreground">
                  {t('admin.audit.noEntries')}
                </td>
              </TableRow>
            )}
          </TableBody>
        </Table>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-muted-foreground">
            {t('common.page', { page: data.page, totalPages: data.totalPages, totalCount: data.totalCount, unit: 'entries' })}
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasPreviousPage}
              onClick={() => setFilters((prev) => ({ ...prev, page: (prev.page ?? 1) - 1 }))}
            >
              <ChevronLeftIcon className="mr-1 size-4" />
              {t('common.previous')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNextPage}
              onClick={() => setFilters((prev) => ({ ...prev, page: (prev.page ?? 1) + 1 }))}
            >
              {t('common.next')}
              <ChevronRightIcon className="ml-1 size-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
