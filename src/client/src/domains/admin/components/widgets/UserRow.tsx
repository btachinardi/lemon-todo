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
  EyeOffIcon,
} from 'lucide-react';
import { TableCell, TableRow } from '@/ui/table';
import { Progress } from '@/ui/progress';
import type { AdminUser, RevealedPii, RevealPiiRequest } from '../../types/admin.types';
import { useRevealPii } from '../../hooks/use-admin-mutations';
import { PiiRevealDialog } from './PiiRevealDialog';

const roleBadgeVariant: Record<string, 'default' | 'secondary' | 'destructive' | 'outline'> = {
  User: 'secondary',
  Admin: 'default',
  SystemAdmin: 'destructive',
};

const REVEAL_DURATION_S = 30;

interface UserRowProps {
  user: AdminUser;
  isSystemAdmin: boolean;
  onAssignRole: (user: AdminUser) => void;
  onRemoveRole: (userId: string, roleName: string) => void;
  onDeactivate: (userId: string) => void;
  onReactivate: (userId: string) => void;
}

/** Single row in the admin user management table with break-the-glass PII reveal. */
export function UserRow({
  user,
  isSystemAdmin,
  onAssignRole,
  onRemoveRole,
  onDeactivate,
  onReactivate,
}: UserRowProps) {
  const { t } = useTranslation();
  const [revealDialogOpen, setRevealDialogOpen] = useState(false);
  const [revealedPii, setRevealedPii] = useState<RevealedPii | null>(null);
  const [secondsRemaining, setSecondsRemaining] = useState(0);
  const revealPii = useRevealPii();

  // Countdown timer: tick every second while PII is revealed
  useEffect(() => {
    if (!revealedPii) return;

    const interval = setInterval(() => {
      setSecondsRemaining((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          setRevealedPii(null);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);

    return () => clearInterval(interval);
  }, [revealedPii]);

  const handleRevealConfirm = (request: RevealPiiRequest) => {
    revealPii.mutate(
      { userId: user.id, request },
      {
        onSuccess: (data) => {
          setRevealedPii(data);
          setSecondsRemaining(REVEAL_DURATION_S);
          setRevealDialogOpen(false);
          revealPii.reset();
        },
      },
    );
  };

  const handleHide = () => {
    setRevealedPii(null);
    setSecondsRemaining(0);
  };

  const displayEmail = revealedPii?.email ?? user.email;
  const displayName = revealedPii?.displayName ?? user.displayName;
  const progressPercent = (secondsRemaining / REVEAL_DURATION_S) * 100;

  return (
    <>
      <TableRow className={!user.isActive ? 'opacity-60' : ''}>
        <TableCell className="font-mono text-xs">{user.id.slice(0, 8)}...</TableCell>
        <TableCell>
          <div>
            <span className={revealedPii ? 'text-amber-600 dark:text-amber-400 font-medium' : ''}>
              {displayEmail}
            </span>
            {revealedPii && <Progress value={progressPercent} className="mt-1 h-0.5" />}
          </div>
        </TableCell>
        <TableCell>
          <div>
            <div className="flex items-center gap-2">
              <span className={revealedPii ? 'text-amber-600 dark:text-amber-400 font-medium' : ''}>
                {displayName}
              </span>
              {revealedPii && (
                <div className="flex items-center gap-1">
                  <Badge variant="outline" className="h-5 px-1.5 text-[10px] font-mono text-amber-600 dark:text-amber-400 border-amber-300 dark:border-amber-700">
                    {t('admin.secureViewer.secondsRemaining', { seconds: secondsRemaining })}
                  </Badge>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="size-5"
                    onClick={handleHide}
                  >
                    <EyeOffIcon className="size-3" />
                    <span className="sr-only">{t('admin.secureViewer.hide')}</span>
                  </Button>
                </div>
              )}
            </div>
            {revealedPii && <Progress value={progressPercent} className="mt-1 h-0.5" />}
          </div>
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
                  <DropdownMenuItem onClick={() => setRevealDialogOpen(true)}>
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

      <PiiRevealDialog
        open={revealDialogOpen}
        onOpenChange={setRevealDialogOpen}
        onReveal={handleRevealConfirm}
        isPending={revealPii.isPending}
        error={revealPii.error}
      />
    </>
  );
}
