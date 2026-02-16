import { useState, useCallback, useMemo } from 'react';
import { useTranslation } from 'react-i18next';
import { Input } from '@/ui/input';
import { Button } from '@/ui/button';
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
import { SearchIcon, ChevronLeftIcon, ChevronRightIcon } from 'lucide-react';
import { Skeleton } from '@/ui/skeleton';
import { useAdminUsers } from '../../hooks/use-admin-queries';
import {
  useAssignRole,
  useRemoveRole,
  useDeactivateUser,
  useReactivateUser,
} from '../../hooks/use-admin-mutations';
import { UserRow } from '../widgets/UserRow';
import { UserCard } from '../widgets/UserCard';
import { RoleAssignmentDialog } from '../widgets/RoleAssignmentDialog';
import type { AdminUser } from '../../types/admin.types';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

const ROLES_FILTER = ['All', 'User', 'Admin', 'SystemAdmin'] as const;

/** Admin user management view with search, role filter, and pagination. */
export function UserManagementView() {
  const { t } = useTranslation();
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState<string>('All');
  const [page, setPage] = useState(1);
  const [assignDialogUser, setAssignDialogUser] = useState<AdminUser | null>(null);

  const roles = useAuthStore((s) => s.user?.roles);
  const isSystemAdmin = useMemo(() => roles?.includes('SystemAdmin') ?? false, [roles]);

  const params = {
    search: search || undefined,
    role: roleFilter !== 'All' ? roleFilter : undefined,
    page,
    pageSize: 10,
  };

  const { data, isLoading } = useAdminUsers(params);
  const assignRole = useAssignRole();
  const removeRole = useRemoveRole();
  const deactivateUser = useDeactivateUser();
  const reactivateUser = useReactivateUser();

  const handleAssignRole = useCallback(
    (userId: string, roleName: string) => {
      assignRole.mutate(
        { userId, roleName },
        { onSuccess: () => setAssignDialogUser(null) },
      );
    },
    [assignRole],
  );

  const handleRemoveRole = useCallback(
    (userId: string, roleName: string) => {
      removeRole.mutate({ userId, roleName });
    },
    [removeRole],
  );

  const handleDeactivate = useCallback(
    (userId: string) => {
      deactivateUser.mutate(userId);
    },
    [deactivateUser],
  );

  const handleReactivate = useCallback(
    (userId: string) => {
      reactivateUser.mutate(userId);
    },
    [reactivateUser],
  );

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center">
        <div className="relative flex-1">
          <SearchIcon className="absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
          <Input
            placeholder={t('admin.users.searchPlaceholder')}
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            className="pl-9"
          />
        </div>
        <Select
          value={roleFilter}
          onValueChange={(v) => {
            setRoleFilter(v);
            setPage(1);
          }}
        >
          <SelectTrigger className="w-40">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            {ROLES_FILTER.map((role) => (
              <SelectItem key={role} value={role}>
                {role === 'All' ? t('admin.users.allRoles') : role}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      {/* Table (desktop) */}
      <div className="hidden rounded-md border sm:block">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead className="w-24">{t('admin.users.columnId')}</TableHead>
              <TableHead>{t('admin.users.columnEmail')}</TableHead>
              <TableHead>{t('admin.users.columnDisplayName')}</TableHead>
              <TableHead>{t('admin.users.columnRoles')}</TableHead>
              <TableHead className="w-28">{t('admin.users.columnStatus')}</TableHead>
              <TableHead className="w-12" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {isLoading ? (
              Array.from({ length: 5 }).map((_, i) => (
                <TableRow key={i}>
                  {Array.from({ length: 6 }).map((__, j) => (
                    <td key={j} className="p-3">
                      <Skeleton className="h-5 w-full" />
                    </td>
                  ))}
                </TableRow>
              ))
            ) : data?.items.length === 0 ? (
              <TableRow>
                <td colSpan={6} className="py-8 text-center text-muted-foreground">
                  {t('admin.users.noUsers')}
                </td>
              </TableRow>
            ) : (
              data?.items.map((u) => (
                <UserRow
                  key={u.id}
                  user={u}
                  isSystemAdmin={isSystemAdmin}
                  onAssignRole={setAssignDialogUser}
                  onRemoveRole={handleRemoveRole}
                  onDeactivate={handleDeactivate}
                  onReactivate={handleReactivate}
                />
              ))
            )}
          </TableBody>
        </Table>
      </div>

      {/* Cards (mobile) */}
      <div className="space-y-3 sm:hidden">
        {isLoading ? (
          Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="rounded-lg border p-4 space-y-2">
              <Skeleton className="h-5 w-3/4" />
              <Skeleton className="h-4 w-1/2" />
              <Skeleton className="h-4 w-1/3" />
            </div>
          ))
        ) : data?.items.length === 0 ? (
          <p className="py-8 text-center text-muted-foreground">
            {t('admin.users.noUsers')}
          </p>
        ) : (
          data?.items.map((u) => (
            <UserCard
              key={u.id}
              user={u}
              isSystemAdmin={isSystemAdmin}
              onAssignRole={setAssignDialogUser}
              onRemoveRole={handleRemoveRole}
              onDeactivate={handleDeactivate}
              onReactivate={handleReactivate}
            />
          ))
        )}
      </div>

      {/* Pagination */}
      {data && data.totalCount > 0 && (
        <div className="flex items-center justify-between">
          <span className="text-sm text-muted-foreground">
            {t('common.page', { page: data.page, totalPages: data.totalPages, totalCount: data.totalCount, unit: t('nav.users').toLowerCase() })}
          </span>
          <div className="flex gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasPreviousPage}
              onClick={() => setPage((p) => p - 1)}
            >
              <ChevronLeftIcon className="mr-1 size-4" />
              {t('common.previous')}
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={!data.hasNextPage}
              onClick={() => setPage((p) => p + 1)}
            >
              {t('common.next')}
              <ChevronRightIcon className="ml-1 size-4" />
            </Button>
          </div>
        </div>
      )}

      {/* Role Assignment Dialog */}
      <RoleAssignmentDialog
        user={assignDialogUser}
        open={!!assignDialogUser}
        onOpenChange={(open) => !open && setAssignDialogUser(null)}
        onAssign={handleAssignRole}
        isPending={assignRole.isPending}
      />
    </div>
  );
}
