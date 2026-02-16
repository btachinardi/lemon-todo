import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';
import { Card, CardContent, CardFooter, CardHeader, CardTitle } from '@/ui/card';

interface ErrorFallbackProps {
  /** The error that was caught */
  error: Error;
  /** Callback to attempt recovery (e.g., reset the error boundary) */
  onReset: () => void;
}

/**
 * Generic error fallback UI displayed when an error boundary catches a rendering crash.
 * Lives in the Design System tier — no domain knowledge, pure visual presentation.
 */
export function ErrorFallback({ error, onReset }: ErrorFallbackProps) {
  const { t } = useTranslation();

  return (
    <Card className="mx-auto mt-12 max-w-md">
      <CardHeader>
        <CardTitle>{t('error.title')}</CardTitle>
      </CardHeader>
      <CardContent>
        <p className="text-sm text-muted-foreground">
          {t('error.description')}
        </p>
        {import.meta.env.DEV && (
          <pre className="mt-4 overflow-auto rounded-lg bg-secondary p-3 font-mono text-xs text-muted-foreground">
            {error.message}
          </pre>
        )}
      </CardContent>
      <CardFooter className="gap-2">
        <Button onClick={onReset} variant="default">
          {t('error.tryAgain')}
        </Button>
        <Button onClick={() => window.location.reload()} variant="outline">
          {t('error.reload')}
        </Button>
      </CardFooter>
    </Card>
  );
}
