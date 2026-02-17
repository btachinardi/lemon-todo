import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { DownloadIcon, XIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import { isInstallAvailable, onInstallAvailable, promptInstall } from '@/lib/pwa';

const DISMISS_KEY = 'pwa-install-dismissed';

function wasDismissed(): boolean {
  try {
    return localStorage.getItem(DISMISS_KEY) === '1';
  } catch {
    return false;
  }
}

/**
 * Shows a dismissible banner when the browser offers a PWA install prompt.
 * Hidden once installed or permanently dismissed by the user.
 * Dismissal is persisted to localStorage so it won't reappear.
 */
export function PWAInstallPrompt() {
  const { t } = useTranslation();
  const [available, setAvailable] = useState(isInstallAvailable);
  const [dismissed, setDismissed] = useState(wasDismissed);

  useEffect(() => onInstallAvailable(setAvailable), []);

  if (!available || dismissed) return null;

  const handleDismiss = () => {
    try {
      localStorage.setItem(DISMISS_KEY, '1');
    } catch {
      // localStorage unavailable — dismiss for this session only
    }
    setDismissed(true);
  };

  return (
    <div
      role="complementary"
      className="fixed bottom-16 left-4 right-4 z-50 mx-auto flex max-w-md items-center gap-3 rounded-lg border bg-card p-4 shadow-lg sm:bottom-4 sm:left-auto sm:right-4"
    >
      <DownloadIcon className="size-5 shrink-0 text-primary" />
      <p className="flex-1 text-sm">{t('pwa.installPrompt')}</p>
      <Button
        size="sm"
        onClick={async () => {
          await promptInstall();
        }}
      >
        {t('pwa.install')}
      </Button>
      <Button
        variant="ghost"
        size="icon"
        className="size-7"
        onClick={handleDismiss}
      >
        <XIcon className="size-4" />
        <span className="sr-only">{t('common.close')}</span>
      </Button>
    </div>
  );
}
