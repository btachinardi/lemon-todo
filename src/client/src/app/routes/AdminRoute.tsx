import type { ReactNode } from 'react';
import { Navigate } from 'react-router';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

/** Props for {@link AdminRoute}. */
interface AdminRouteProps {
  children: ReactNode;
}

/**
 * Route guard for admin pages. Redirects to login if not authenticated,
 * or to home if the user lacks Admin or SystemAdmin role.
 */
export function AdminRoute({ children }: AdminRouteProps) {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);
  const roles = useAuthStore((s) => s.user?.roles);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  const isAdmin = roles?.some((r) => r === 'Admin' || r === 'SystemAdmin') ?? false;
  if (!isAdmin) {
    return <Navigate to="/board" replace />;
  }

  return <>{children}</>;
}
