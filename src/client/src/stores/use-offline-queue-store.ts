/**
 * Zustand store that tracks the offline mutation queue state and
 * orchestrates draining (replaying) queued mutations when connectivity resumes.
 *
 * Flow:
 *   1. User makes a mutation while offline → `enqueueMutation()` stores it in IndexedDB
 *   2. Browser emits `online` event → `drain()` replays mutations FIFO
 *   3. Before replay, a silent token refresh is attempted (tokens may have expired)
 *   4. Each mutation is replayed as a raw fetch; 409 conflicts are discarded with a toast
 *   5. After drain, TanStack Query caches are invalidated to refetch server truth
 */
import { create } from 'zustand';
import { toast } from 'sonner';
import * as offlineQueue from '@/lib/offline-queue';
import { API_BASE_URL } from '@/lib/api-client';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

interface OfflineQueueState {
  /** Number of mutations waiting in the IndexedDB queue. */
  pendingCount: number;
  /** Whether a drain (replay) is currently in progress. */
  isSyncing: boolean;
  /** Result of the last drain attempt. Resets to null after 3 seconds. */
  lastSyncResult: 'success' | 'partial' | null;
}

interface OfflineQueueActions {
  /** Re-reads the queue count from IndexedDB. */
  refreshCount: () => Promise<void>;
  /** Enqueues a mutation and updates the count. */
  enqueueMutation: (
    method: offlineQueue.QueuedMutation['method'],
    url: string,
    body?: unknown,
  ) => Promise<string>;
  /** Replays all queued mutations in FIFO order. */
  drain: () => Promise<void>;
}

export const useOfflineQueueStore = create<OfflineQueueState & OfflineQueueActions>()(
  (set, get) => ({
    pendingCount: 0,
    isSyncing: false,
    lastSyncResult: null,

    refreshCount: async () => {
      try {
        const c = await offlineQueue.count();
        set({ pendingCount: c });
      } catch {
        // IndexedDB unavailable — keep current count
      }
    },

    enqueueMutation: async (method, url, body) => {
      const id = await offlineQueue.enqueue(method, url, body);
      await get().refreshCount();
      return id;
    },

    drain: async () => {
      const { isSyncing } = get();
      if (isSyncing) return;

      set({ isSyncing: true, lastSyncResult: null });

      try {
        // Attempt silent token refresh before replaying (token may have expired)
        try {
          const response = await fetch(`${API_BASE_URL}/api/auth/refresh`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            credentials: 'include',
          });
          if (response.ok) {
            const data = (await response.json()) as {
              accessToken: string;
              user: { id: string; email: string; displayName: string; roles: string[] };
            };
            useAuthStore.getState().setAuth(data.accessToken, data.user);
          }
        } catch {
          // Refresh failed — try replaying with current token anyway
        }

        const mutations = await offlineQueue.dequeueAll();
        if (mutations.length === 0) {
          set({ isSyncing: false });
          return;
        }

        let hadConflicts = false;

        for (const mutation of mutations) {
          try {
            const token = useAuthStore.getState().accessToken;
            const headers: Record<string, string> = {
              'Content-Type': 'application/json',
            };
            if (token) {
              headers['Authorization'] = `Bearer ${token}`;
            }

            const response = await fetch(`${API_BASE_URL}${mutation.url}`, {
              method: mutation.method,
              headers,
              credentials: 'include',
              body: mutation.body,
            });

            if (response.status === 409) {
              // Conflict — discard mutation, notify user
              hadConflicts = true;
              toast.warning('A change was discarded due to a conflict. The latest version is shown.');
            } else if (!response.ok && response.status >= 500) {
              // Server error — stop draining, keep remaining mutations
              toast.error('Sync interrupted by a server error. Will retry later.');
              break;
            }
            // Remove from queue regardless (2xx or 4xx client error)
            await offlineQueue.remove(mutation.id);
          } catch {
            // Network error during drain — stop, keep remaining
            toast.error('Connection lost during sync. Will retry when online.');
            break;
          }
        }

        await get().refreshCount();

        const result = hadConflicts ? 'partial' : 'success';
        set({ lastSyncResult: result });

        // Notify listeners (e.g. QueryProvider) to invalidate caches
        window.dispatchEvent(new CustomEvent('offline-queue-drained'));

        // Clear the success indicator after 3 seconds
        setTimeout(() => {
          if (get().lastSyncResult === result) {
            set({ lastSyncResult: null });
          }
        }, 3000);
      } finally {
        set({ isSyncing: false });
      }
    },
  }),
);

/**
 * Initializes the offline queue by reading the current count and
 * setting up the `online` event listener for automatic drain.
 * Also drains immediately if the app starts online with pending mutations
 * (e.g. user closed the app while offline, then reopened with connection).
 * Call once at app startup.
 */
export function initOfflineQueue(): void {
  const store = useOfflineQueueStore.getState();

  // Read initial count, then drain if online with pending mutations
  store.refreshCount().then(() => {
    if (navigator.onLine && useOfflineQueueStore.getState().pendingCount > 0) {
      useOfflineQueueStore.getState().drain();
    }
  });

  // Drain on reconnect
  window.addEventListener('online', () => {
    useOfflineQueueStore.getState().drain();
  });
}
