import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { DownloadIcon, LogOutIcon, ShieldCheckIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/ui/dropdown-menu';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/ui/popover';
import { Separator } from '@/ui/separator';
import { useAuthStore } from '../stores/use-auth-store';
import { useLogout } from '../hooks/use-auth-mutations';
import { useRevealOwnProfile } from '../hooks/use-reveal-own-profile';
import { useDevAccountPassword } from '../hooks/use-dev-account-password';
import { SelfRevealDialog } from './SelfRevealDialog';
import { isInstallAvailable, onInstallAvailable, promptInstall } from '@/lib/pwa';

interface UserMenuProps {
  /** When "inline", renders user info and actions directly (for mobile sheets). Defaults to "dropdown". */
  variant?: 'dropdown' | 'inline';
}

/** Header dropdown menu showing user info and sign-out action. Supports inline rendering for mobile sheets. */
export function UserMenu({ variant = 'dropdown' }: UserMenuProps) {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const logout = useLogout();
  const revealMutation = useRevealOwnProfile();
  const devPassword = useDevAccountPassword();
  const [revealDialogOpen, setRevealDialogOpen] = useState(false);
  const [installAvailable, setInstallAvailable] = useState(isInstallAvailable);

  useEffect(() => onInstallAvailable(setInstallAvailable), []);

  if (!user) return null;

  const initials = user.displayName
    .split(' ')
    .map((w) => w[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);

  const handleRevealDialogChange = (open: boolean) => {
    setRevealDialogOpen(open);
    if (!open) revealMutation.reset();
  };

  if (variant === 'inline') {
    return (
      <>
        <div className="flex flex-col gap-3">
          <div className="flex items-center gap-3 px-3 py-2">
            <span className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary/20 text-sm font-semibold text-lemon">
              {initials}
            </span>
            <div className="flex min-w-0 flex-col">
              <p className="truncate text-sm font-medium">{user.displayName}</p>
              <p className="truncate text-xs text-muted-foreground">{user.email}</p>
            </div>
          </div>
          <Button
            variant="ghost"
            size="sm"
            className="justify-start gap-2 px-3 text-muted-foreground hover:text-emerald-600 dark:hover:text-emerald-400"
            onClick={() => setRevealDialogOpen(true)}
          >
            <ShieldCheckIcon className="size-4" />
            {t('auth.selfReveal.revealButton')}
          </Button>
          {installAvailable && (
            <Button
              variant="ghost"
              size="sm"
              className="justify-start gap-2 px-3 text-muted-foreground hover:text-primary"
              onClick={() => promptInstall()}
            >
              <DownloadIcon className="size-4" />
              {t('pwa.installApp')}
            </Button>
          )}
          <Separator />
          <Button
            variant="ghost"
            size="sm"
            className="justify-start gap-2 px-3 text-destructive hover:text-destructive"
            onClick={() => logout.mutate()}
            disabled={logout.isPending}
          >
            <LogOutIcon className="size-4" />
            {logout.isPending ? t('auth.userMenu.signingOut') : t('auth.userMenu.signOut')}
          </Button>
        </div>

        <SelfRevealDialog
          open={revealDialogOpen}
          onOpenChange={handleRevealDialogChange}
          onReveal={(password) => revealMutation.mutate(password)}
          isPending={revealMutation.isPending}
          error={revealMutation.error}
          revealedEmail={revealMutation.data?.email}
          revealedDisplayName={revealMutation.data?.displayName}
          devPassword={devPassword}
        />
      </>
    );
  }

  return (
    <>
      <DropdownMenu>
        <DropdownMenuTrigger asChild>
          <Button
            variant="ghost"
            size="sm"
            className="gap-2 text-muted-foreground hover:text-foreground"
          >
            <span className="flex size-8 items-center justify-center rounded-full bg-primary/20 text-xs font-semibold text-lemon sm:size-7">
              {initials}
            </span>
            <span className="hidden text-sm sm:inline">{user.displayName}</span>
          </Button>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuLabel className="font-normal">
            <Popover>
              <PopoverTrigger asChild>
                <button
                  type="button"
                  className="flex w-full cursor-pointer flex-col gap-1 rounded-sm p-0 text-left transition-colors hover:text-emerald-600 dark:hover:text-emerald-400"
                  data-testid="user-info-trigger"
                >
                  <p className="text-sm font-medium">{user.displayName}</p>
                  <p className="text-xs text-muted-foreground">{user.email}</p>
                </button>
              </PopoverTrigger>
              <PopoverContent side="left" className="w-72">
                <div className="flex flex-col gap-3">
                  <div className="flex items-start gap-2">
                    <ShieldCheckIcon className="mt-0.5 size-5 shrink-0 text-emerald-500" />
                    <p className="text-sm text-muted-foreground">
                      {t('auth.selfReveal.popoverHint')}
                    </p>
                  </div>
                  <Button
                    size="sm"
                    variant="outline"
                    onClick={() => setRevealDialogOpen(true)}
                    className="w-full"
                  >
                    {t('auth.selfReveal.revealButton')}
                  </Button>
                </div>
              </PopoverContent>
            </Popover>
          </DropdownMenuLabel>
          <DropdownMenuSeparator />
          {installAvailable && (
            <DropdownMenuItem onClick={() => promptInstall()}>
              <DownloadIcon className="mr-2 size-4" />
              {t('pwa.installApp')}
            </DropdownMenuItem>
          )}
          <DropdownMenuItem
            onClick={() => logout.mutate()}
            disabled={logout.isPending}
            className="text-destructive focus:text-destructive"
          >
            <LogOutIcon className="mr-2 size-4" />
            {logout.isPending ? t('auth.userMenu.signingOut') : t('auth.userMenu.signOut')}
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>

      <SelfRevealDialog
        open={revealDialogOpen}
        onOpenChange={handleRevealDialogChange}
        onReveal={(password) => revealMutation.mutate(password)}
        isPending={revealMutation.isPending}
        error={revealMutation.error}
        revealedEmail={revealMutation.data?.email}
        revealedDisplayName={revealMutation.data?.displayName}
        devPassword={devPassword}
      />
    </>
  );
}
