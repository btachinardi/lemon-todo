import { useState, type FormEvent } from 'react';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { ApiRequestError } from '@/lib/api-client';
import { useLogin } from '../hooks/use-auth-mutations';

interface LoginFormProps {
  onSuccess: () => void;
}

/** Email + password form for user login. */
export function LoginForm({ onSuccess }: LoginFormProps) {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);

  const login = useLogin();

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    login.mutate(
      { email, password },
      {
        onSuccess,
        onError: (err) => {
          if (err instanceof ApiRequestError) {
            setError(
              err.status === 401
                ? 'Invalid email or password.'
                : err.apiError.title,
            );
          } else {
            setError('Something went wrong. Please try again.');
          }
        },
      },
    );
  }

  return (
    <form onSubmit={handleSubmit} className="space-y-4">
      {error && (
        <div className="rounded-md border border-destructive/50 bg-destructive/10 px-4 py-3 text-sm text-destructive">
          {error}
        </div>
      )}
      <div className="space-y-2">
        <label htmlFor="login-email" className="text-sm font-medium text-foreground">
          Email
        </label>
        <Input
          id="login-email"
          type="email"
          placeholder="you@example.com"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
          autoFocus
        />
      </div>
      <div className="space-y-2">
        <label htmlFor="login-password" className="text-sm font-medium text-foreground">
          Password
        </label>
        <Input
          id="login-password"
          type="password"
          placeholder="Your password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          autoComplete="current-password"
        />
      </div>
      <Button type="submit" className="w-full" disabled={login.isPending}>
        {login.isPending ? 'Signing in...' : 'Sign in'}
      </Button>
    </form>
  );
}
