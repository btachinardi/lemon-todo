import { useTranslation } from 'react-i18next';
import { CloudOffIcon, LoaderCircleIcon, CheckCircle2Icon } from 'lucide-react';
import { useOfflineQueueStore } from '@/stores/use-offline-queue-store';
import { useNetworkStatus } from '@/hooks/use-network-status';

/**
 * Displays the sync status of the offline mutation queue.
 * - Shows "X changes pending" when offline with queued mutations.
 * - Shows "Syncing..." during drain.
 * - Shows "All synced" briefly after a successful drain.
 * - Hidden when online with nothing to sync.
 */
export function SyncIndicator() {
  const { t } = useTranslation();
  const isOnline = useNetworkStatus();
  const pendingCount = useOfflineQueueStore((s) => s.pendingCount);
  const isSyncing = useOfflineQueueStore((s) => s.isSyncing);
  const lastSyncResult = useOfflineQueueStore((s) => s.lastSyncResult);

  if (isSyncing) {
    return (
      <div
        className="flex items-center gap-1.5 text-xs text-muted-foreground"
        role="status"
        aria-live="polite"
      >
        <LoaderCircleIcon className="size-3 animate-spin" />
        {t('offline.syncing')}
      </div>
    );
  }

  if (lastSyncResult === 'success') {
    return (
      <div
        className="flex items-center gap-1.5 text-xs text-green-600 dark:text-green-400"
        role="status"
        aria-live="polite"
      >
        <CheckCircle2Icon className="size-3" />
        {t('offline.allSynced')}
      </div>
    );
  }

  if (!isOnline && pendingCount > 0) {
    return (
      <div
        className="flex items-center gap-1.5 text-xs text-amber-600 dark:text-amber-400"
        role="status"
        aria-live="polite"
      >
        <CloudOffIcon className="size-3" />
        {t('offline.pendingChanges', { count: pendingCount })}
      </div>
    );
  }

  return null;
}
