import { Link, useNavigate } from 'react-router';
import { LoginForm } from '@/domains/auth/components/LoginForm';

/** Login page with link to register. */
export function LoginPage() {
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center">
        <h2 className="text-xl font-semibold text-foreground">Welcome back</h2>
        <p className="text-sm text-muted-foreground">Sign in to your account</p>
      </div>
      <LoginForm onSuccess={() => navigate('/', { replace: true })} />
      <p className="text-center text-sm text-muted-foreground">
        Don&apos;t have an account?{' '}
        <Link to="/register" className="font-medium text-primary hover:underline">
          Create one
        </Link>
      </p>
    </div>
  );
}
