import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

interface PublicRouteProps {
  children: ReactNode;
}

/** Redirects authenticated users to the board. Passes unauthenticated users through. */
export function PublicRoute({ children }: PublicRouteProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (isAuthenticated) {
    return <Navigate to="/board" replace />;
  }

  return <>{children}</>;
}
