import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { DownloadIcon, XIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import { isInstallAvailable, onInstallAvailable, promptInstall } from '@/lib/pwa';

/**
 * Shows a dismissible banner when the browser offers a PWA install prompt.
 * Hidden once installed or dismissed by the user.
 */
export function PWAInstallPrompt() {
  const { t } = useTranslation();
  const [available, setAvailable] = useState(isInstallAvailable);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => onInstallAvailable(setAvailable), []);

  if (!available || dismissed) return null;

  return (
    <div
      role="complementary"
      className="fixed bottom-4 left-4 right-4 z-50 mx-auto flex max-w-md items-center gap-3 rounded-lg border bg-card p-4 shadow-lg sm:left-auto sm:right-4"
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
        onClick={() => setDismissed(true)}
      >
        <XIcon className="size-4" />
        <span className="sr-only">{t('common.close')}</span>
      </Button>
    </div>
  );
}
