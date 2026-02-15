import { Navigate } from 'react-router';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';
import { AuthLayout } from '../layouts/AuthLayout';
import { RegisterPage } from '../pages/RegisterPage';

/** Route wrapper for registration. Redirects to home if already authenticated. */
export function RegisterRoute() {
  const isAuthenticated = useAuthStore((s) => s.isAuthenticated);

  if (isAuthenticated) {
    return <Navigate to="/" replace />;
  }

  return (
    <AuthLayout>
      <RegisterPage />
    </AuthLayout>
  );
}
