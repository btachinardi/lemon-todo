import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { ApiRequestError } from '@/lib/api-client';
import { useLogin } from '../hooks/use-auth-mutations';

interface LoginFormProps {
  /** Called after successful authentication and token storage. */
  onSuccess: () => void;
}

/** Email + password form for user login. */
export function LoginForm({ onSuccess }: LoginFormProps) {
  const { t } = useTranslation();
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
            if (err.status === 401) {
              setError(t('auth.login.errorInvalid'));
            } else if (err.status === 429) {
              setError(t('auth.login.errorLocked'));
            } else {
              setError(err.apiError.title);
            }
          } else {
            setError(t('common.error.generic'));
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
          {t('auth.fields.email')}
        </label>
        <Input
          id="login-email"
          type="email"
          placeholder={t('auth.fields.emailPlaceholder')}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
          autoFocus
        />
      </div>
      <div className="space-y-2">
        <label htmlFor="login-password" className="text-sm font-medium text-foreground">
          {t('auth.fields.password')}
        </label>
        <Input
          id="login-password"
          type="password"
          placeholder={t('auth.fields.passwordPlaceholder')}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          autoComplete="current-password"
        />
      </div>
      <Button type="submit" className="w-full" disabled={login.isPending}>
        {login.isPending ? t('auth.login.submitting') : t('auth.login.submit')}
      </Button>
    </form>
  );
}
