import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router';
import { LoginForm } from '@/domains/auth/components/LoginForm';
import { DevAccountSwitcher } from '@/domains/auth/components/DevAccountSwitcher';

/**
 * Login page with email/password form validation.
 * Redirects to the home page on successful authentication.
 */
export function LoginPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center">
        <h2 className="text-xl font-semibold text-foreground">{t('auth.login.title')}</h2>
        <p className="text-sm text-muted-foreground">{t('auth.login.subtitle')}</p>
      </div>
      <LoginForm onSuccess={() => navigate('/board', { replace: true })} />
      <p className="text-center text-sm text-muted-foreground">
        {t('auth.login.noAccount')}{' '}
        <Link to="/register" className="font-medium text-lemon hover:underline">
          {t('auth.login.createOne')}
        </Link>
      </p>
      <DevAccountSwitcher />
    </div>
  );
}
