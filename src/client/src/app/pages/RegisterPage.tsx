import { Link, useNavigate } from 'react-router';
import { RegisterForm } from '@/domains/auth/components/RegisterForm';

/** Register page with link to login. */
export function RegisterPage() {
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center">
        <h2 className="text-xl font-semibold text-foreground">Create your account</h2>
        <p className="text-sm text-muted-foreground">Start organizing your tasks</p>
      </div>
      <RegisterForm onSuccess={() => navigate('/', { replace: true })} />
      <p className="text-center text-sm text-muted-foreground">
        Already have an account?{' '}
        <Link to="/login" className="font-medium text-primary hover:underline">
          Sign in
        </Link>
      </p>
    </div>
  );
}
