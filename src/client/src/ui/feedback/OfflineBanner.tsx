import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { WifiOffIcon } from 'lucide-react';
import { useNetworkStatus } from '@/hooks/use-network-status';

/**
 * Renders a fixed banner at the top of the viewport when the browser is offline.
 * Shows a softer "Viewing cached data" message when TanStack Query has cached data,
 * or the stronger "You are offline" when no cache is available.
 * Automatically hides when connectivity is restored.
 */
export function OfflineBanner() {
  const { t } = useTranslation();
  const isOnline = useNetworkStatus();
  const queryClient = useQueryClient();

  if (isOnline) return null;

  // Check if we have cached task/board data
  const hasCachedData = queryClient.getQueryCache().getAll()
    .some((q) => q.state.data !== undefined);

  return (
    <div
      role="alert"
      className={`fixed top-0 inset-x-0 z-50 px-4 py-2 text-center text-base font-medium flex items-center justify-center gap-2 ${
        hasCachedData
          ? 'bg-amber-500/90 text-amber-950'
          : 'bg-destructive text-destructive-foreground'
      }`}
    >
      <WifiOffIcon className="size-3.5" />
      {hasCachedData ? t('offline.cachedData') : t('offline.banner')}
    </div>
  );
}
