import type { ReactNode } from 'react';
import { NavLink } from 'react-router';
import { UsersIcon, ScrollTextIcon, ArrowLeftIcon } from 'lucide-react';
import { Toaster } from 'sonner';
import { cn } from '@/lib/utils';
import { UserMenu } from '@/domains/auth/components/UserMenu';
import { ThemeToggle } from '@/domains/tasks/components/atoms/ThemeToggle';
import { useThemeStore, resolveTheme } from '@/stores/use-theme-store';

interface AdminLayoutProps {
  children: ReactNode;
}

/** Admin panel layout with sidebar navigation for Users and Audit Log sections. */
export function AdminLayout({ children }: AdminLayoutProps) {
  const theme = useThemeStore((s) => s.theme);
  const resolvedTheme = resolveTheme(theme);

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-50 border-b border-border/40 bg-background/90 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-3 sm:h-16 sm:px-6">
          <div className="flex items-center gap-3">
            <h1 className="font-mono text-base font-light tracking-normal sm:text-lg">
              <span className="text-foreground">LEMON</span>
              <span className="text-primary">DO</span>
              <span className="ml-2 text-xs text-muted-foreground">ADMIN</span>
            </h1>
          </div>
          <nav
            className="flex items-center gap-1 rounded-lg border-2 border-border/40 bg-secondary/30 p-1"
            aria-label="Admin navigation"
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
              <span className="hidden sm:inline">Users</span>
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
              <span className="hidden sm:inline">Audit Log</span>
            </NavLink>
          </nav>
          <div className="flex items-center gap-1">
            <NavLink
              to="/"
              className="inline-flex items-center gap-1 rounded-md px-2 py-1.5 text-sm text-muted-foreground hover:text-foreground"
            >
              <ArrowLeftIcon className="size-3.5" />
              <span className="hidden sm:inline">Back</span>
            </NavLink>
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
