export type SyncStatus = 'synced' | 'pending' | 'not-synced';

interface UseSaveIndicatorParams {
  /** Whether a mutation is currently in flight. */
  isPending: boolean;
  /** Whether the browser is online. */
  isOnline: boolean;
  /** Number of mutations queued in the offline queue. */
  pendingCount: number;
  /** Currently selected task ID. */
  taskId: string | null;
}

/**
 * Derives a persistent sync indicator from mutation state and offline queue.
 *
 * - `'pending'`    — a mutation is in flight
 * - `'not-synced'` — offline queue has pending mutations (device offline or draining)
 * - `'synced'`     — all changes are saved to the server
 *
 * Unlike the old transient indicator, this value is always meaningful and
 * the UI should always display it.
 */
export function useSaveIndicator({
  isPending,
  isOnline: _isOnline,
  pendingCount,
  taskId: _taskId,
}: UseSaveIndicatorParams): SyncStatus {
  if (isPending) return 'pending';
  if (pendingCount > 0) return 'not-synced';
  return 'synced';
}
