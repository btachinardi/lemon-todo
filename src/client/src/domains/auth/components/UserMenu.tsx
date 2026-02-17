import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { LogOutIcon, ShieldCheckIcon } from 'lucide-react';
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
import { useAuthStore } from '../stores/use-auth-store';
import { useLogout } from '../hooks/use-auth-mutations';
import { useRevealOwnProfile } from '../hooks/use-reveal-own-profile';
import { useDevAccountPassword } from '../hooks/use-dev-account-password';
import { SelfRevealDialog } from './SelfRevealDialog';

/** Header dropdown menu showing user info and sign-out action. */
export function UserMenu() {
  const { t } = useTranslation();
  const user = useAuthStore((s) => s.user);
  const logout = useLogout();
  const revealMutation = useRevealOwnProfile();
  const devPassword = useDevAccountPassword();
  const [revealDialogOpen, setRevealDialogOpen] = useState(false);

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
