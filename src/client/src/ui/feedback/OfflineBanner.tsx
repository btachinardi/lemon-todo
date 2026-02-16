import { useTranslation } from 'react-i18next';
import { useNetworkStatus } from '@/hooks/use-network-status';

/**
 * Renders a fixed banner at the top of the viewport when the browser is offline.
 * Automatically hides when connectivity is restored.
 */
export function OfflineBanner() {
  const { t } = useTranslation();
  const isOnline = useNetworkStatus();

  if (isOnline) return null;

  return (
    <div
      role="alert"
      className="fixed top-0 inset-x-0 z-50 bg-destructive text-destructive-foreground px-4 py-2 text-center text-sm font-medium"
    >
      {t('offline.banner')}
    </div>
  );
}
