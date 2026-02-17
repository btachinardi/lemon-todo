import { useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { ApiRequestError } from '@/lib/api-client';
import { useRegister } from '../hooks/use-auth-mutations';
import { PasswordStrengthMeter, evaluatePasswordStrength } from './PasswordStrengthMeter';

interface RegisterFormProps {
  /** Called after successful authentication and token storage. */
  onSuccess: () => void;
}

/** Registration form with email, password (with strength meter), and display name. */
export function RegisterForm({ onSuccess }: RegisterFormProps) {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const register = useRegister();

  const allRequirementsMet = password.length > 0
    && evaluatePasswordStrength(password).checks
      .filter((c) => c.required)
      .every((c) => c.passed);

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
        <div className="relative">
          <Input
            id="register-password"
            type={showPassword ? 'text' : 'password'}
            placeholder={t('auth.fields.passwordHint')}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
            autoComplete="new-password"
            className="pr-10"
          />
          <button
            type="button"
            onClick={() => setShowPassword(!showPassword)}
            className="absolute right-0 top-0 flex h-full items-center px-3 text-muted-foreground transition-colors hover:text-foreground"
            aria-label={t(showPassword ? 'auth.fields.hidePassword' : 'auth.fields.showPassword')}
            tabIndex={-1}
          >
            {showPassword ? <EyeOffIcon /> : <EyeIcon />}
          </button>
        </div>
        <PasswordStrengthMeter password={password} />
      </div>
      <Button
        type="submit"
        className="w-full"
        disabled={register.isPending || (password.length > 0 && !allRequirementsMet)}
      >
        {register.isPending ? t('auth.register.submitting') : t('auth.register.submit')}
      </Button>
    </form>
  );
}

function EyeIcon() {
  return (
    <svg className="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M2.062 12.348a1 1 0 0 1 0-.696 10.75 10.75 0 0 1 19.876 0 1 1 0 0 1 0 .696 10.75 10.75 0 0 1-19.876 0" />
      <circle cx="12" cy="12" r="3" />
    </svg>
  );
}

function EyeOffIcon() {
  return (
    <svg className="size-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
      <path d="M10.733 5.076a10.744 10.744 0 0 1 11.205 6.575 1 1 0 0 1 0 .696 10.747 10.747 0 0 1-1.444 2.49" />
      <path d="M14.084 14.158a3 3 0 0 1-4.242-4.242" />
      <path d="M17.479 17.499a10.75 10.75 0 0 1-15.417-5.151 1 1 0 0 1 0-.696 10.75 10.75 0 0 1 4.446-5.143" />
      <path d="m2 2 20 20" />
    </svg>
  );
}
