import type { ReactNode } from 'react';
import { NavLink } from 'react-router';
import { KanbanIcon, ListIcon } from 'lucide-react';
import { Toaster } from 'sonner';
import { cn } from '@/lib/utils';

interface DashboardLayoutProps {
  children: ReactNode;
}

export function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <div className="flex min-h-screen flex-col">
      <header className="border-b">
        <div className="flex h-14 items-center gap-6 px-4 sm:px-6">
          <h1 className="text-lg font-bold">LemonDo</h1>
          <nav className="flex items-center gap-1" aria-label="View switcher">
            <NavLink
              to="/"
              end
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-secondary text-secondary-foreground'
                    : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                )
              }
            >
              <KanbanIcon className="size-4" />
              Board
            </NavLink>
            <NavLink
              to="/list"
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-3 py-1.5 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-secondary text-secondary-foreground'
                    : 'text-muted-foreground hover:bg-accent hover:text-accent-foreground',
                )
              }
            >
              <ListIcon className="size-4" />
              List
            </NavLink>
          </nav>
        </div>
      </header>
      <main className="flex-1">{children}</main>
      <Toaster />
    </div>
  );
}
