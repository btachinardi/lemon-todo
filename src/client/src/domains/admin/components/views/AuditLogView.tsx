import { useState } from 'react';
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
          <label className="text-xs font-medium text-muted-foreground">From</label>
          <Input
            type="date"
            className="w-40"
            value={filters.dateFrom ?? ''}
            onChange={(e) => updateFilter({ dateFrom: e.target.value || undefined })}
          />
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">To</label>
          <Input
            type="date"
            className="w-40"
            value={filters.dateTo ?? ''}
            onChange={(e) => updateFilter({ dateTo: e.target.value || undefined })}
          />
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">Action</label>
          <Select
            value={filters.action ?? '__all__'}
            onValueChange={(v) => updateFilter({ action: v === '__all__' ? undefined : (v as AuditAction) })}
          >
            <SelectTrigger className="w-44">
              <SelectValue placeholder="All actions" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">All actions</SelectItem>
              {AUDIT_ACTIONS.map((action) => (
                <SelectItem key={action} value={action}>
                  {action}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1">
          <label className="text-xs font-medium text-muted-foreground">Resource</label>
          <Select
            value={filters.resourceType ?? '__all__'}
            onValueChange={(v) => updateFilter({ resourceType: v === '__all__' ? undefined : v })}
          >
            <SelectTrigger className="w-32">
              <SelectValue placeholder="All" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="__all__">All</SelectItem>
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
              <TableHead className="w-44">Timestamp</TableHead>
              <TableHead className="w-36">Action</TableHead>
              <TableHead className="w-24">Resource</TableHead>
              <TableHead className="w-28">Resource ID</TableHead>
              <TableHead className="w-28">Actor</TableHead>
              <TableHead>Details</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              <TableRow>
                <td colSpan={6} className="py-8 text-center text-muted-foreground">
                  Loading...
                </td>
              </TableRow>
            ) : data && data.items.length > 0 ? (
              data.items.map((entry) => <AuditLogRow key={entry.id} entry={entry} />)
            ) : (
              <TableRow>
                <td colSpan={6} className="py-8 text-center text-muted-foreground">
                  No audit entries found.
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
            Page {data.page} of {data.totalPages} ({data.totalCount} entries)
          </p>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasPreviousPage}
              onClick={() => setFilters((prev) => ({ ...prev, page: (prev.page ?? 1) - 1 }))}
            >
              <ChevronLeftIcon className="mr-1 size-4" />
              Previous
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNextPage}
              onClick={() => setFilters((prev) => ({ ...prev, page: (prev.page ?? 1) + 1 }))}
            >
              Next
              <ChevronRightIcon className="ml-1 size-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  );
}
