import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { ApiRequestError } from '@/lib/api-client';
import { useRegister } from '../hooks/use-auth-mutations';

interface RegisterFormProps {
  onSuccess: () => void;
}

/** Registration form with email, password, and display name. */
export function RegisterForm({ onSuccess }: RegisterFormProps) {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [error, setError] = useState<string | null>(null);

  const register = useRegister();

  function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);

    register.mutate(
      { email, password, displayName },
      {
        onSuccess,
        onError: (err) => {
          if (err instanceof ApiRequestError) {
            if (err.status === 409) {
              setError(t('auth.register.errorExists'));
            } else if (err.apiError.errors) {
              const messages = Object.values(err.apiError.errors).flat();
              setError(messages.join(' '));
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
        <label htmlFor="register-name" className="text-sm font-medium text-foreground">
          {t('auth.fields.displayName')}
        </label>
        <Input
          id="register-name"
          type="text"
          placeholder={t('auth.fields.displayNamePlaceholder')}
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
          required
          autoComplete="name"
          autoFocus
        />
      </div>
      <div className="space-y-2">
        <label htmlFor="register-email" className="text-sm font-medium text-foreground">
          {t('auth.fields.email')}
        </label>
        <Input
          id="register-email"
          type="email"
          placeholder={t('auth.fields.emailPlaceholder')}
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          required
          autoComplete="email"
        />
      </div>
      <div className="space-y-2">
        <label htmlFor="register-password" className="text-sm font-medium text-foreground">
          {t('auth.fields.password')}
        </label>
        <Input
          id="register-password"
          type="password"
          placeholder={t('auth.fields.passwordHint')}
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          required
          minLength={8}
          autoComplete="new-password"
        />
      </div>
      <Button type="submit" className="w-full" disabled={register.isPending}>
        {register.isPending ? t('auth.register.submitting') : t('auth.register.submit')}
      </Button>
    </form>
  );
}
