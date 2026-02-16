import type { ReactNode } from 'react';
import { NavLink } from 'react-router';
import { useTranslation } from 'react-i18next';
import { KanbanIcon, ListIcon, ShieldIcon } from 'lucide-react';
import { Toaster } from 'sonner';
import { cn } from '@/lib/utils';
import { UserMenu } from '@/domains/auth/components/UserMenu';
import { ThemeToggle } from '@/domains/tasks/components/atoms/ThemeToggle';
import { LanguageSwitcher } from '@/domains/tasks/components/atoms/LanguageSwitcher';
import { useThemeStore, resolveTheme } from '@/stores/use-theme-store';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

interface DashboardLayoutProps {
  children: ReactNode;
}

/** App shell with branded header, pill-shaped view switcher, and toast container. */
export function DashboardLayout({ children }: DashboardLayoutProps) {
  const { t } = useTranslation();
  const theme = useThemeStore((s) => s.theme);
  const resolvedTheme = resolveTheme(theme);
  const roles = useAuthStore((s) => s.user?.roles);
  const isAdmin = roles?.some((r) => r === 'Admin' || r === 'SystemAdmin') ?? false;

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-50 border-b border-border/40 bg-background/90 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-3 sm:h-16 sm:px-6">
          <h1 className="font-mono text-base font-light tracking-normal sm:text-lg">
            <span className="text-foreground">{t('brand.lemon')}</span>
            <span className="text-primary">{t('brand.do')}</span>
          </h1>
          <nav
            className="flex items-center gap-1 rounded-lg border-2 border-border/40 bg-secondary/30 p-1"
            aria-label={t('nav.viewSwitcher')}
          >
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-semibold transition-all duration-300 sm:px-3.5',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <KanbanIcon className="size-3.5" />
              <span className="hidden sm:inline">{t('nav.board')}</span>
            </NavLink>
            <NavLink
              to="/list"
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-semibold transition-all duration-300 sm:px-3.5',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <ListIcon className="size-3.5" />
              <span className="hidden sm:inline">{t('nav.list')}</span>
            </NavLink>
          </nav>
          <div className="flex items-center gap-1">
            {isAdmin && (
              <NavLink
                to="/admin/users"
                className="inline-flex items-center gap-1 rounded-md px-2 py-1.5 text-xs text-muted-foreground hover:text-foreground"
              >
                <ShieldIcon className="size-3" />
                <span className="hidden sm:inline">{t('nav.admin')}</span>
              </NavLink>
            )}
            <LanguageSwitcher />
            <ThemeToggle
              theme={theme}
              onToggle={() => {
                const themes: Array<typeof theme> = ['light', 'dark', 'system'];
                const idx = themes.indexOf(theme);
                const next = themes[(idx + 1) % themes.length];
                useThemeStore.getState().setTheme(next);
              }}
            />
            <UserMenu />
          </div>
        </div>
      </header>
      <main className="mx-auto w-full max-w-7xl flex-1">{children}</main>
      <footer className="pointer-events-none fixed bottom-2 right-3">
        <span className="text-[10px] text-muted-foreground/40 select-none">
          v{__APP_VERSION__}
        </span>
      </footer>
      <Toaster theme={resolvedTheme} />
    </div>
  );
}
