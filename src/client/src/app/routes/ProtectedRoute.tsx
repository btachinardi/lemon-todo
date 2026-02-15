import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

interface ProtectedRouteProps {
  children: ReactNode;
}

/** Redirects unauthenticated users to the login page. */
export function ProtectedRoute({ children }: ProtectedRouteProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
