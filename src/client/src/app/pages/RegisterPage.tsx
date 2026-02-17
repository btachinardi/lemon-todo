import { useTranslation } from 'react-i18next';
import { Link, useNavigate } from 'react-router';
import { RegisterForm } from '@/domains/auth/components/RegisterForm';

/**
 * Registration page with email, password, and display name form validation.
 * Redirects to the home page on successful account creation.
 */
export function RegisterPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <div className="space-y-6">
      <div className="space-y-2 text-center">
        <h2 className="text-xl font-semibold text-foreground">{t('auth.register.title')}</h2>
        <p className="text-base text-muted-foreground">{t('auth.register.subtitle')}</p>
      </div>
      <RegisterForm onSuccess={() => navigate('/board', { replace: true })} />
      <p className="text-center text-base text-muted-foreground">
        {t('auth.register.hasAccount')}{' '}
        <Link to="/login" className="font-medium text-highlight hover:underline">
          {t('auth.register.signIn')}
        </Link>
      </p>
    </div>
  );
}
