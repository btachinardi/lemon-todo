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
      <header className="sticky top-0 z-50 border-b border-border/50 bg-background/80 backdrop-blur-lg">
        <div className="flex h-16 items-center justify-between px-6">
          <h1 className="font-display text-xl font-bold tracking-tight">
            <span className="text-foreground">Lemon</span>
            <span className="text-primary">Do</span>
          </h1>
          <nav
            className="flex items-center gap-1 rounded-full border border-border/50 bg-secondary/50 p-1"
            aria-label="View switcher"
          >
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-full px-4 py-1.5 text-sm font-medium transition-all duration-200',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_12px_rgba(169,255,3,0.3)]'
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
                  'inline-flex items-center gap-1.5 rounded-full px-4 py-1.5 text-sm font-medium transition-all duration-200',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_12px_rgba(169,255,3,0.3)]'
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
      <main className="flex-1">{children}</main>
      <Toaster theme="dark" />
    </div>
  );
}
