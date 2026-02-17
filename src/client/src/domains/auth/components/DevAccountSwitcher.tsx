import { useState } from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { FlaskConicalIcon, ShieldIcon, CrownIcon, UserIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useDemoAccountsEnabled } from '@/domains/config/hooks/use-config';
import { authApi } from '../api/auth.api';
import { useAuthStore } from '../stores/use-auth-store';

interface DevAccount {
  email: string;
  password: string;
  roleKey: string;
  /** i18n key for the role label (e.g., "auth.devSwitcher.roles.user"). */
  labelKey: string;
  /** i18n key for the role description (e.g., "auth.devSwitcher.roles.userDesc"). */
  descKey: string;
  icon: typeof UserIcon;
  /** Tailwind classes for color accent (text, background, border). */
  accent: string;
}

// eslint-disable-next-line react-refresh/only-export-components
export const DEV_ACCOUNTS: DevAccount[] = [
  {
    email: 'dev.user@lemondo.dev',
    password: 'User1234',
    roleKey: 'user',
    labelKey: 'auth.devSwitcher.roles.user',
    descKey: 'auth.devSwitcher.roles.userDesc',
    icon: UserIcon,
    accent: 'text-blue-700 dark:text-blue-400 bg-blue-500/10 border-blue-500/20',
  },
  {
    email: 'dev.admin@lemondo.dev',
    password: 'Admin1234',
    roleKey: 'admin',
    labelKey: 'auth.devSwitcher.roles.admin',
    descKey: 'auth.devSwitcher.roles.adminDesc',
    icon: ShieldIcon,
    accent: 'text-amber-800 dark:text-amber-400 bg-amber-500/10 border-amber-500/20',
  },
  {
    email: 'dev.sysadmin@lemondo.dev',
    password: 'SysAdmin1234',
    roleKey: 'sysadmin',
    labelKey: 'auth.devSwitcher.roles.sysadmin',
    descKey: 'auth.devSwitcher.roles.sysadminDesc',
    icon: CrownIcon,
    accent: 'text-rose-700 dark:text-rose-400 bg-rose-500/10 border-rose-500/20',
  },
];

/** Quick login selector for seeded demo accounts (controlled by feature flag). */
export function DevAccountSwitcher() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const setAuth = useAuthStore((s) => s.setAuth);
  const logout = useAuthStore((s) => s.logout);
  const [switchingRole, setSwitchingRole] = useState<string | null>(null);
  const { data: demoEnabled } = useDemoAccountsEnabled();

  if (!demoEnabled) return null;

  async function handleSwitch(account: DevAccount) {
    setSwitchingRole(account.roleKey);
    try {
      if (isAuthenticated) {
        try {
          await authApi.logout();
        } catch {
          // Server logout may fail if token is already expired — clear local state and proceed
        }
        logout();
      }

      const response = await authApi.login({
        email: account.email,
        password: account.password,
      });
      setAuth(response.accessToken, response.user);
      navigate('/board', { replace: true });
    } catch {
      // Login failed — stay on current page
    } finally {
      setSwitchingRole(null);
    }
  }

  const isSwitching = switchingRole !== null;

  return (
    <div className="space-y-3">
      <div className="flex items-center gap-2">
        <div className="h-px flex-1 bg-border" />
        <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
          <FlaskConicalIcon className="size-3" />
          <span>{t('auth.devSwitcher.title')}</span>
        </div>
        <div className="h-px flex-1 bg-border" />
      </div>
      <p className="text-center text-[11px] text-muted-foreground">
        {t('auth.devSwitcher.subtitle')}
      </p>
      <div className="grid gap-2 overflow-hidden">
        {DEV_ACCOUNTS.map((account) => {
          const Icon = account.icon;
          const isActive = switchingRole === account.roleKey;
          return (
            <button
              key={account.roleKey}
              type="button"
              disabled={isSwitching}
              onClick={() => handleSwitch(account)}
              className={cn(
                'group flex items-center gap-3 rounded-lg border px-3 py-2.5 text-left transition-all',
                'hover:bg-secondary/60 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring',
                'disabled:pointer-events-none disabled:opacity-50',
                account.accent,
              )}
            >
              <span
                className={cn(
                  'flex size-8 shrink-0 items-center justify-center rounded-md',
                  account.accent,
                )}
              >
                <Icon className="size-4" />
              </span>
              <div className="min-w-0 flex-1">
                <p className="text-sm font-medium text-foreground">
                  {isActive ? t('auth.devSwitcher.switching') : t(account.labelKey)}
                </p>
                <p className="truncate text-xs text-muted-foreground">
                  {t(account.descKey)}
                </p>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}
