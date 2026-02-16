import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Badge } from '@/ui/badge';
import { Button } from '@/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/ui/dropdown-menu';
import {
  MoreHorizontalIcon,
  ShieldIcon,
  ShieldOffIcon,
  UserXIcon,
  UserCheckIcon,
  EyeIcon,
} from 'lucide-react';
import { TableCell, TableRow } from '@/ui/table';
import type { AdminUser, RevealedPii } from '../../types/admin.types';
import { useRevealPii } from '../../hooks/use-admin-mutations';

const roleBadgeVariant: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  User: 'secondary',
  Admin: 'default',
  SystemAdmin: 'destructive',
};

const REVEAL_DURATION_MS = 30_000;

interface UserRowProps {
  user: AdminUser;
  isSystemAdmin: boolean;
  onAssignRole: (user: AdminUser) => void;
  onRemoveRole: (userId: string, roleName: string) => void;
  onDeactivate: (userId: string) => void;
  onReactivate: (userId: string) => void;
}

/** Single row in the admin user management table. */
export function UserRow({
  user,
  isSystemAdmin,
  onAssignRole,
  onRemoveRole,
  onDeactivate,
  onReactivate,
}: UserRowProps) {
  const { t } = useTranslation();
  const [revealedPii, setRevealedPii] = useState<RevealedPii | null>(null);
  const revealPii = useRevealPii();

  // Auto-hide revealed PII after 30 seconds
  useEffect(() => {
    if (!revealedPii) return;
    const timer = setTimeout(() => setRevealedPii(null), REVEAL_DURATION_MS);
    return () => clearTimeout(timer);
  }, [revealedPii]);

  const handleReveal = async () => {
    const result = await revealPii.mutateAsync(user.id);
    setRevealedPii(result);
  };

  const displayEmail = revealedPii?.email ?? user.email;
  const displayName = revealedPii?.displayName ?? user.displayName;

  return (
    <TableRow className={!user.isActive ? 'opacity-60' : ''}>
      <TableCell className="font-mono text-xs">{user.id.slice(0, 8)}...</TableCell>
      <TableCell>
        <span className={revealedPii ? 'text-amber-600 dark:text-amber-400 font-medium' : ''}>
          {displayEmail}
        </span>
      </TableCell>
      <TableCell>
        <span className={revealedPii ? 'text-amber-600 dark:text-amber-400 font-medium' : ''}>
          {displayName}
        </span>
      </TableCell>
      <TableCell>
        <div className="flex flex-wrap gap-1">
          {user.roles.map((role) => (
            <Badge
              key={role}
              variant={roleBadgeVariant[role] ?? 'outline'}
              className="text-xs"
            >
              {role}
            </Badge>
          ))}
        </div>
      </TableCell>
      <TableCell>
        <Badge variant={user.isActive ? 'secondary' : 'destructive'} className="text-xs">
          {user.isActive ? t('admin.users.active') : t('admin.users.deactivated')}
        </Badge>
      </TableCell>
      <TableCell>
        {isSystemAdmin && (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon" className="size-8">
                <MoreHorizontalIcon className="size-4" />
                <span className="sr-only">{t('common.actions')}</span>
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              {!revealedPii && (
                <DropdownMenuItem
                  onClick={handleReveal}
                  disabled={revealPii.isPending}
                >
                  <EyeIcon className="mr-2 size-4" />
                  {t('admin.users.revealPii')}
                </DropdownMenuItem>
              )}
              <DropdownMenuItem onClick={() => onAssignRole(user)}>
                <ShieldIcon className="mr-2 size-4" />
                {t('admin.users.assignRole')}
              </DropdownMenuItem>
              {user.roles.length > 1 && (
                <DropdownMenuItem
                  onClick={() => {
                    // Remove the highest role that isn't "User"
                    const removable = user.roles.filter((r) => r !== 'User');
                    if (removable.length > 0) {
                      onRemoveRole(user.id, removable[removable.length - 1]);
                    }
                  }}
                >
                  <ShieldOffIcon className="mr-2 size-4" />
                  {t('admin.users.removeRole')}
                </DropdownMenuItem>
              )}
              {user.isActive ? (
                <DropdownMenuItem
                  onClick={() => onDeactivate(user.id)}
                  className="text-destructive focus:text-destructive"
                >
                  <UserXIcon className="mr-2 size-4" />
                  {t('admin.users.deactivate')}
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={() => onReactivate(user.id)}>
                  <UserCheckIcon className="mr-2 size-4" />
                  {t('admin.users.reactivate')}
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        )}
      </TableCell>
    </TableRow>
  );
}
