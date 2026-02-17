import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { useOfflineQueueStore, initOfflineQueue } from './use-offline-queue-store';
import type { QueuedMutation } from '@/lib/offline-queue';

// Mock the offline-queue module (IndexedDB doesn't exist in jsdom)
const mockMutations: QueuedMutation[] = [];
vi.mock('@/lib/offline-queue', () => ({
  enqueue: vi.fn(async (method: string, url: string, body?: unknown) => {
    const id = `mock-${Date.now()}`;
    mockMutations.push({
      id,
      timestamp: Date.now(),
      method: method as QueuedMutation['method'],
      url,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return id;
  }),
  dequeueAll: vi.fn(async () => [...mockMutations]),
  remove: vi.fn(async (id: string) => {
    const idx = mockMutations.findIndex((m) => m.id === id);
    if (idx >= 0) mockMutations.splice(idx, 1);
  }),
  clear: vi.fn(async () => {
    mockMutations.length = 0;
  }),
  count: vi.fn(async () => mockMutations.length),
}));

// Mock toast
vi.mock('sonner', () => ({
  toast: {
    info: vi.fn(),
    warning: vi.fn(),
    error: vi.fn(),
  },
}));

// Mock auth store
vi.mock('@/domains/auth/stores/use-auth-store', () => ({
  useAuthStore: {
    getState: () => ({
      accessToken: 'test-token',
      setAuth: vi.fn(),
    }),
  },
}));

// Mock api-client
vi.mock('@/lib/api-client', () => ({
  API_BASE_URL: '',
}));

describe('useOfflineQueueStore', () => {
  beforeEach(() => {
    mockMutations.length = 0;
    useOfflineQueueStore.setState({
      pendingCount: 0,
      isSyncing: false,
      lastSyncResult: null,
    });
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  describe('enqueueMutation', () => {
    it('should add mutation to queue and update count', async () => {
      const store = useOfflineQueueStore.getState();

      await store.enqueueMutation('PUT', '/api/tasks/123', { title: 'Updated' });

      expect(useOfflineQueueStore.getState().pendingCount).toBe(1);
      expect(mockMutations).toHaveLength(1);
      expect(mockMutations[0].method).toBe('PUT');
      expect(mockMutations[0].url).toBe('/api/tasks/123');
    });

    it('should increment count for each enqueued mutation', async () => {
      const store = useOfflineQueueStore.getState();

      await store.enqueueMutation('POST', '/api/tasks', { title: 'Task 1' });
      await store.enqueueMutation('POST', '/api/tasks', { title: 'Task 2' });

      expect(useOfflineQueueStore.getState().pendingCount).toBe(2);
    });
  });

  describe('drain', () => {
    it('should replay mutations via fetch in FIFO order', async () => {
      const fetchSpy = vi.fn().mockResolvedValue({ ok: true, status: 200 });
      vi.stubGlobal('fetch', fetchSpy);

      // Enqueue two mutations
      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('PUT', '/api/tasks/1', { title: 'First' });
      await store.enqueueMutation('DELETE', '/api/tasks/2');

      await useOfflineQueueStore.getState().drain();

      // First call is the token refresh attempt, then 2 mutation replays
      expect(fetchSpy).toHaveBeenCalledTimes(3);
      // Mutation 1
      expect(fetchSpy).toHaveBeenNthCalledWith(2, '/api/tasks/1', expect.objectContaining({
        method: 'PUT',
      }));
      // Mutation 2
      expect(fetchSpy).toHaveBeenNthCalledWith(3, '/api/tasks/2', expect.objectContaining({
        method: 'DELETE',
      }));

      expect(useOfflineQueueStore.getState().pendingCount).toBe(0);
      expect(useOfflineQueueStore.getState().lastSyncResult).toBe('success');

      vi.unstubAllGlobals();
    });

    it('should discard 409 conflict mutations with a warning toast', async () => {
      const { toast } = await import('sonner');
      const fetchSpy = vi.fn()
        .mockResolvedValueOnce({ ok: true, status: 200 }) // refresh
        .mockResolvedValueOnce({ ok: false, status: 409 }); // conflict
      vi.stubGlobal('fetch', fetchSpy);

      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('PUT', '/api/tasks/1', { title: 'Conflict' });

      await useOfflineQueueStore.getState().drain();

      expect(toast.warning).toHaveBeenCalled();
      expect(useOfflineQueueStore.getState().pendingCount).toBe(0);
      expect(useOfflineQueueStore.getState().lastSyncResult).toBe('partial');

      vi.unstubAllGlobals();
    });

    it('should stop draining on server error and keep remaining mutations', async () => {
      const { toast } = await import('sonner');
      const fetchSpy = vi.fn()
        .mockResolvedValueOnce({ ok: true, status: 200 }) // refresh
        .mockResolvedValueOnce({ ok: true, status: 200 }) // mutation 1 succeeds
        .mockResolvedValueOnce({ ok: false, status: 500 }); // mutation 2 server error
      vi.stubGlobal('fetch', fetchSpy);

      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('PUT', '/api/tasks/1', { title: 'OK' });
      await store.enqueueMutation('PUT', '/api/tasks/2', { title: 'Fail' });

      await useOfflineQueueStore.getState().drain();

      expect(toast.error).toHaveBeenCalled();
      // First mutation removed, second still pending
      expect(useOfflineQueueStore.getState().pendingCount).toBe(1);

      vi.unstubAllGlobals();
    });

    it('should not start a second drain while one is in progress', async () => {
      let drainResolve: () => void;
      const blockingPromise = new Promise<void>((resolve) => { drainResolve = resolve; });

      const fetchSpy = vi.fn().mockImplementation(async () => {
        await blockingPromise;
        return { ok: true, status: 200 };
      });
      vi.stubGlobal('fetch', fetchSpy);

      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('PUT', '/api/tasks/1', { title: 'Test' });

      // Start first drain (blocks on fetch)
      const drain1 = useOfflineQueueStore.getState().drain();
      expect(useOfflineQueueStore.getState().isSyncing).toBe(true);

      // Try second drain — should no-op
      const drain2 = useOfflineQueueStore.getState().drain();

      // Unblock
      drainResolve!();
      await drain1;
      await drain2;

      // Only one set of fetches should have happened (refresh + 1 mutation)
      expect(fetchSpy).toHaveBeenCalledTimes(2);

      vi.unstubAllGlobals();
    });

    it('should set isSyncing to false after drain completes', async () => {
      const fetchSpy = vi.fn().mockResolvedValue({ ok: true, status: 200 });
      vi.stubGlobal('fetch', fetchSpy);

      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('POST', '/api/tasks/1/complete');

      await useOfflineQueueStore.getState().drain();

      expect(useOfflineQueueStore.getState().isSyncing).toBe(false);

      vi.unstubAllGlobals();
    });

    it('should dispatch offline-queue-drained event after successful drain', async () => {
      const fetchSpy = vi.fn().mockResolvedValue({ ok: true, status: 200 });
      vi.stubGlobal('fetch', fetchSpy);

      const eventHandler = vi.fn();
      window.addEventListener('offline-queue-drained', eventHandler);

      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('POST', '/api/tasks', { title: 'Test' });

      await useOfflineQueueStore.getState().drain();

      expect(eventHandler).toHaveBeenCalledTimes(1);

      window.removeEventListener('offline-queue-drained', eventHandler);
      vi.unstubAllGlobals();
    });

    it('should dispatch offline-queue-drained event even after partial drain with conflicts', async () => {
      const fetchSpy = vi.fn()
        .mockResolvedValueOnce({ ok: true, status: 200 }) // refresh
        .mockResolvedValueOnce({ ok: false, status: 409 }); // conflict
      vi.stubGlobal('fetch', fetchSpy);

      const eventHandler = vi.fn();
      window.addEventListener('offline-queue-drained', eventHandler);

      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('PUT', '/api/tasks/1', { title: 'Conflict' });

      await useOfflineQueueStore.getState().drain();

      expect(eventHandler).toHaveBeenCalledTimes(1);

      window.removeEventListener('offline-queue-drained', eventHandler);
      vi.unstubAllGlobals();
    });

    it('should not dispatch offline-queue-drained when queue is empty', async () => {
      const fetchSpy = vi.fn().mockResolvedValue({ ok: true, status: 200 });
      vi.stubGlobal('fetch', fetchSpy);

      const eventHandler = vi.fn();
      window.addEventListener('offline-queue-drained', eventHandler);

      // Drain with empty queue
      await useOfflineQueueStore.getState().drain();

      expect(eventHandler).not.toHaveBeenCalled();

      window.removeEventListener('offline-queue-drained', eventHandler);
      vi.unstubAllGlobals();
    });
  });

  describe('initOfflineQueue', () => {
    it('should register an online event listener', () => {
      const addEventSpy = vi.spyOn(window, 'addEventListener');

      initOfflineQueue();

      expect(addEventSpy).toHaveBeenCalledWith('online', expect.any(Function));
    });

    it('should drain on startup when already online with pending mutations', async () => {
      // Simulate online state with pending mutations
      Object.defineProperty(navigator, 'onLine', { value: true, writable: true, configurable: true });

      // Pre-seed the queue with a mutation
      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('POST', '/api/tasks', { title: 'Pending' });
      expect(useOfflineQueueStore.getState().pendingCount).toBe(1);

      const fetchSpy = vi.fn().mockResolvedValue({ ok: true, status: 200 });
      vi.stubGlobal('fetch', fetchSpy);

      initOfflineQueue();

      // Allow the async drain to complete
      await vi.waitFor(() => {
        expect(useOfflineQueueStore.getState().pendingCount).toBe(0);
      });

      // Fetch should have been called (token refresh + mutation replay)
      expect(fetchSpy.mock.calls.length).toBeGreaterThanOrEqual(2);

      vi.unstubAllGlobals();
    });

    it('should not drain on startup when offline', async () => {
      Object.defineProperty(navigator, 'onLine', { value: false, writable: true, configurable: true });

      // Pre-seed the queue
      const store = useOfflineQueueStore.getState();
      await store.enqueueMutation('POST', '/api/tasks', { title: 'Pending' });

      const fetchSpy = vi.fn().mockResolvedValue({ ok: true, status: 200 });
      vi.stubGlobal('fetch', fetchSpy);

      initOfflineQueue();

      // Give async operations time to settle
      await new Promise((r) => setTimeout(r, 100));

      // Drain should not have fired — only refreshCount reads the count
      // The mutation is still pending
      expect(useOfflineQueueStore.getState().pendingCount).toBe(1);
      // Only the refreshCount read, no fetch calls for drain
      expect(fetchSpy).not.toHaveBeenCalled();

      vi.unstubAllGlobals();
    });

    it('should not drain on startup when online but queue is empty', async () => {
      Object.defineProperty(navigator, 'onLine', { value: true, writable: true, configurable: true });

      const fetchSpy = vi.fn().mockResolvedValue({ ok: true, status: 200 });
      vi.stubGlobal('fetch', fetchSpy);

      initOfflineQueue();

      // Give async operations time to settle
      await new Promise((r) => setTimeout(r, 100));

      // No drain should fire (queue is empty)
      expect(fetchSpy).not.toHaveBeenCalled();

      vi.unstubAllGlobals();
    });
  });
});
