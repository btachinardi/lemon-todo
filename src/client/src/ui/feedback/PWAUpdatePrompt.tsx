import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { RefreshCwIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import { applyUpdate, onUpdateAvailable } from '@/lib/pwa';

/**
 * Shows a toast-style banner when a new version of the app is available.
 * Clicking "Update" reloads the page to apply the new service worker.
 */
export function PWAUpdatePrompt() {
  const { t } = useTranslation();
  const [updateReady, setUpdateReady] = useState(false);

  useEffect(() => onUpdateAvailable(setUpdateReady), []);

  if (!updateReady) return null;

  return (
    <div
      role="alert"
      className="fixed bottom-4 left-4 right-4 z-50 mx-auto flex max-w-md items-center gap-3 rounded-lg border bg-card p-4 shadow-lg sm:left-auto sm:right-4"
    >
      <RefreshCwIcon className="size-5 shrink-0 text-primary" />
      <p className="flex-1 text-sm">{t('pwa.updateAvailable')}</p>
      <Button size="sm" onClick={() => applyUpdate()}>
        {t('pwa.update')}
      </Button>
    </div>
  );
}
