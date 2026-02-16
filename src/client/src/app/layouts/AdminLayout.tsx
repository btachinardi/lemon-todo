import type { ReactNode } from 'react';
import { NavLink } from 'react-router';
import { useTranslation } from 'react-i18next';
import { UsersIcon, ScrollTextIcon, ArrowLeftIcon } from 'lucide-react';
import { Toaster } from 'sonner';
import { cn } from '@/lib/utils';
import { UserMenu } from '@/domains/auth/components/UserMenu';
import { ThemeToggle } from '@/domains/tasks/components/atoms/ThemeToggle';
import { LanguageSwitcher } from '@/domains/tasks/components/atoms/LanguageSwitcher';
import { useThemeStore, resolveTheme } from '@/stores/use-theme-store';

/** Props for {@link AdminLayout}. */
interface AdminLayoutProps {
  children: ReactNode;
}

/** Admin panel layout with sidebar navigation for Users and Audit Log sections. */
export function AdminLayout({ children }: AdminLayoutProps) {
  const { t } = useTranslation();
  const theme = useThemeStore((s) => s.theme);
  const resolvedTheme = resolveTheme(theme);

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-50 border-b border-border/40 bg-background/90 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-3 sm:h-16 sm:px-6">
          <div className="flex items-center gap-3">
            <h1 className="flex items-center gap-1.5">
              <img src="/lemondo-icon.png" alt="" className="size-7 sm:size-8" />
              <span className="font-[var(--font-brand)] text-lg font-black tracking-tight sm:text-xl">
                <span className="text-foreground">{t('brand.lemon')}</span>
                <span className="text-lemon">{t('brand.do')}</span>
              </span>
              <span className="ml-1 text-xs text-muted-foreground">{t('brand.admin')}</span>
            </h1>
          </div>
          <nav
            className="flex items-center gap-1 rounded-lg border-2 border-border/40 bg-secondary/30 p-1"
            aria-label={t('nav.adminNav')}
          >
            <NavLink
              to="/admin/users"
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-semibold transition-all duration-300 sm:px-3.5',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <UsersIcon className="size-3.5" />
              <span className="hidden sm:inline">{t('nav.users')}</span>
            </NavLink>
            <NavLink
              to="/admin/audit"
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-semibold transition-all duration-300 sm:px-3.5',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <ScrollTextIcon className="size-3.5" />
              <span className="hidden sm:inline">{t('nav.auditLog')}</span>
            </NavLink>
          </nav>
          <div className="flex items-center gap-1">
            <NavLink
              to="/board"
              className="inline-flex items-center gap-1 rounded-md px-2 py-1.5 text-sm text-muted-foreground hover:text-foreground"
            >
              <ArrowLeftIcon className="size-3.5" />
              <span className="hidden sm:inline">{t('common.back')}</span>
            </NavLink>
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
      <main className="mx-auto w-full max-w-7xl flex-1 p-4 sm:p-6">{children}</main>
      <Toaster theme={resolvedTheme} />
    </div>
  );
}
