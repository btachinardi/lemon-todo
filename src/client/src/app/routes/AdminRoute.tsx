import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

interface AdminRouteProps {
  children: ReactNode;
}

/**
 * Route guard for admin pages. Redirects to home if user is not authenticated.
 * Server-side authorization handles role checking — this is just a basic auth guard.
 * If the user lacks admin privileges, the API will return 403.
 */
export function AdminRoute({ children }: AdminRouteProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
