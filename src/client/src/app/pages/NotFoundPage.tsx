import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';

/** Catch-all 404 page with a link back to the home route. */
export function NotFoundPage() {
  const { t } = useTranslation();

  return (
    <div className="flex min-h-screen flex-col items-center justify-center gap-4">
      <h1 className="text-6xl font-bold text-highlight">{t('notFound.code')}</h1>
      <p className="text-muted-foreground">{t('notFound.title')}</p>
      <Button asChild>
        <Link to="/">{t('notFound.goHome')}</Link>
      </Button>
    </div>
  );
}
