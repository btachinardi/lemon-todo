import { Navigate } from 'react-router';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';
import { AuthLayout } from '../layouts/AuthLayout';
import { LoginPage } from '../pages/LoginPage';

/** Route wrapper for login. Redirects to home if already authenticated. */
export function LoginRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <AuthLayout>
      <LoginPage />
    </AuthLayout>
  );
}
