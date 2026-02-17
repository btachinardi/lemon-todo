import { useState } from 'react';
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
import type { AdminUser, RevealProtectedDataRequest } from '../../types/admin.types';
import { useRevealProtectedData } from '../../hooks/use-admin-mutations';
import { useDevAccountPassword } from '@/domains/auth/hooks/use-dev-account-password';
import { ProtectedDataRevealDialog } from './ProtectedDataRevealDialog';

const roleBadgeVariant: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  User: 'secondary',
  Admin: 'default',
  SystemAdmin: 'destructive',
};

interface UserCardProps {
  user: AdminUser;
  isSystemAdmin: boolean;
  onAssignRole: (user: AdminUser) => void;
  onRemoveRole: (userId: string, roleName: string) => void;
  onDeactivate: (userId: string) => void;
  onReactivate: (userId: string) => void;
}

/** Mobile card layout for a single user in the admin user management view. */
export function UserCard({
  user,
  isSystemAdmin,
  onAssignRole,
  onRemoveRole,
  onDeactivate,
  onReactivate,
}: UserCardProps) {
  const { t } = useTranslation();
  const [revealDialogOpen, setRevealDialogOpen] = useState(false);
  const revealProtectedData = useRevealProtectedData();
  const devPassword = useDevAccountPassword();

  const handleRevealConfirm = (request: RevealProtectedDataRequest) => {
    revealProtectedData.mutate(
      { userId: user.id, request },
      { onSuccess: () => setRevealDialogOpen(false) },
    );
  };

  return (
    <>
      <div className={`rounded-lg border p-4 space-y-2 ${!user.isActive ? 'opacity-60' : ''}`}>
        <div className="flex items-start justify-between">
          <div className="min-w-0 flex-1">
            <p className="truncate text-base font-medium">{user.displayName}</p>
            <p className="truncate text-sm text-muted-foreground">{user.email}</p>
          </div>
          {isSystemAdmin && (
            <DropdownMenu>
              <DropdownMenuTrigger asChild>
                <Button variant="ghost" size="icon" className="size-8 shrink-0">
                  <MoreHorizontalIcon className="size-4" />
                  <span className="sr-only">{t('common.actions')}</span>
                </Button>
              </DropdownMenuTrigger>
              <DropdownMenuContent align="end">
                <DropdownMenuItem onClick={() => setRevealDialogOpen(true)}>
                  <EyeIcon className="mr-2 size-4" />
                  {t('admin.users.revealProtectedData')}
                </DropdownMenuItem>
                <DropdownMenuItem onClick={() => onAssignRole(user)}>
                  <ShieldIcon className="mr-2 size-4" />
                  {t('admin.users.assignRole')}
                </DropdownMenuItem>
                {user.roles.length > 1 && (
                  <DropdownMenuItem
                    onClick={() => {
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
        </div>
        <div className="flex flex-wrap items-center gap-1.5">
          {user.roles.map((role) => (
            <Badge
              key={role}
              variant={roleBadgeVariant[role] ?? 'outline'}
              className="text-sm"
            >
              {role}
            </Badge>
          ))}
          <Badge variant={user.isActive ? 'secondary' : 'destructive'} className="text-sm">
            {user.isActive ? t('admin.users.active') : t('admin.users.deactivated')}
          </Badge>
        </div>
        <p className="font-mono text-[10px] text-muted-foreground">{user.id.slice(0, 8)}...</p>
      </div>

      <ProtectedDataRevealDialog
        open={revealDialogOpen}
        onOpenChange={setRevealDialogOpen}
        onReveal={handleRevealConfirm}
        isPending={revealProtectedData.isPending}
        error={revealProtectedData.error}
        devPassword={devPassword}
      />
    </>
  );
}
