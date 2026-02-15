import type { ReactNode } from 'react';
import { NavLink } from 'react-router';
import { KanbanIcon, ListIcon } from 'lucide-react';
import { Toaster } from 'sonner';
import { cn } from '@/lib/utils';

interface DashboardLayoutProps {
  children: ReactNode;
}

/** App shell with branded header, pill-shaped view switcher, and toast container. */
export function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-50 border-b border-border/40 bg-background/90 backdrop-blur-xl">
        <div className="mx-auto flex h-16 max-w-7xl items-center justify-between px-6">
          <h1 className="font-mono text-lg font-light tracking-normal">
            <span className="text-foreground">LEMON</span>
            <span className="text-primary">DO</span>
          </h1>
          <nav
            className="flex items-center gap-1 rounded-lg border-2 border-border/40 bg-secondary/30 p-1"
            aria-label="View switcher"
          >
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-3.5 py-1.5 text-sm font-semibold transition-all duration-300',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <KanbanIcon className="size-3.5" />
              Board
            </NavLink>
            <NavLink
              to="/list"
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-3.5 py-1.5 text-sm font-semibold transition-all duration-300',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <ListIcon className="size-3.5" />
              List
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="mx-auto w-full max-w-7xl flex-1">{children}</main>
      <Toaster theme="dark" />
    </div>
  );
}
