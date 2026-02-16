import { test, expect, type Page, type BrowserContext } from '@playwright/test';
import { loginViaApi } from '../helpers/auth.helpers';
import { createTask, listTasks } from '../helpers/api.helpers';
import { completeOnboarding } from '../helpers/onboarding.helpers';

test.describe.serial('Offline Banner', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Offline test task' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('no offline banner when connected', async () => {
    await page.goto('/board');
    await expect(page.getByText('Offline test task')).toBeVisible();

    // No offline alert should be present
    await expect(page.getByRole('alert')).not.toBeVisible();
  });

  test('offline banner appears when disconnected', async () => {
    // Simulate going offline
    await context.setOffline(true);

    // Dispatch the offline event explicitly (Playwright's setOffline doesn't always fire the event)
    await page.evaluate(() => window.dispatchEvent(new Event('offline')));

    // Wait for the offline event to be detected
    await page.waitForTimeout(1000);

    // The offline banner should appear
    await expect(page.getByRole('alert')).toBeVisible({ timeout: 5000 });
  });

  test('offline banner disappears when reconnected', async () => {
    // Go back online
    await context.setOffline(false);

    // Dispatch the online event and wait for navigator.onLine to update
    await page.evaluate(() => {
      // Force navigator.onLine to be true (Playwright's setOffline should handle this,
      // but dispatch the event to ensure React's useSyncExternalStore picks it up)
      window.dispatchEvent(new Event('online'));
    });

    // The banner uses useSyncExternalStore(subscribe, () => navigator.onLine),
    // so it should update reactively. Give it time for the React render cycle.
    await page.waitForTimeout(2000);

    // Check navigator.onLine is actually true
    const isOnline = await page.evaluate(() => navigator.onLine);
    expect(isOnline).toBe(true);

    // The offline banner (role=alert) should no longer be visible
    await expect(page.getByRole('alert')).not.toBeVisible({ timeout: 5000 });
  });
});

test.describe.serial('Offline Read Support', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
    await createTask({ title: 'Cached offline task' });
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('data loads while online', async () => {
    await page.goto('/board');
    await expect(page.getByText('Cached offline task')).toBeVisible();
  });

  test('cached data visible when going offline', async () => {
    // Now go offline — TanStack Query should still show cached data
    await context.setOffline(true);

    // Reload the page — service worker should serve cached responses
    // Note: full navigation may fail, so we test without reload
    await page.waitForTimeout(500);

    // The task should still be visible from React Query cache
    await expect(page.getByText('Cached offline task')).toBeVisible();
  });

  test('back to online restores full functionality', async () => {
    await context.setOffline(false);
    // Dispatch the online event explicitly
    await page.evaluate(() => window.dispatchEvent(new Event('online')));

    // Wait a moment for the network to stabilize
    await page.waitForTimeout(1000);

    // Reload the page — the AuthHydrationProvider will do a silent refresh
    const refreshPromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/auth/refresh'),
      { timeout: 15000 },
    ).catch(() => null);
    await page.reload();
    await refreshPromise;

    // Wait for the authenticated layout to load
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible', timeout: 10000 });
    await expect(page.getByText('Cached offline task')).toBeVisible({ timeout: 10000 });
  });
});

test.describe.serial('Offline Mutation Queue', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('queued mutation drains and creates task on reconnect', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Simulate offline state and directly enqueue a mutation into IndexedDB.
    // TanStack Query pauses mutations when navigator.onLine is false (networkMode: 'online'),
    // so we bypass the UI form and enqueue directly — testing the drain/sync mechanism.
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'onLine', { value: false, writable: true, configurable: true });
      window.dispatchEvent(new Event('offline'));
    });
    await page.waitForTimeout(500);

    // Enqueue a create-task mutation directly into the IndexedDB offline queue
    await page.evaluate(async () => {
      const dbName = 'lemondo-offline';
      const storeName = 'mutations';
      const db: IDBDatabase = await new Promise((resolve, reject) => {
        const req = indexedDB.open(dbName, 1);
        req.onupgradeneeded = () => {
          if (!req.result.objectStoreNames.contains(storeName)) {
            req.result.createObjectStore(storeName, { keyPath: 'id' });
          }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
      });

      const mutation = {
        id: crypto.randomUUID(),
        timestamp: Date.now(),
        method: 'POST',
        url: '/api/tasks',
        body: JSON.stringify({ title: 'Offline queued task' }),
      };

      const tx = db.transaction(storeName, 'readwrite');
      tx.objectStore(storeName).add(mutation);
      await new Promise<void>((resolve, reject) => {
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
      });
      db.close();
    });

    // Go back online — the 'online' event triggers the store's drain()
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'onLine', { value: true, writable: true, configurable: true });
      window.dispatchEvent(new Event('online'));
    });

    // Wait for the drain to replay the mutation and caches to invalidate
    await page.waitForTimeout(5000);

    // Verify the task now exists on the server via API helper
    const { items } = await listTasks();
    const synced = items.find((t) => t.title === 'Offline queued task');
    expect(synced).toBeTruthy();

    // Reload to see the task on the board
    const refreshPromise = page.waitForResponse(
      (resp) => resp.url().includes('/api/auth/refresh'),
      { timeout: 15000 },
    ).catch(() => null);
    await page.reload();
    await refreshPromise;

    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible', timeout: 10000 });
    await expect(page.getByText('Offline queued task')).toBeVisible({ timeout: 10000 });
  });
});

test.describe.serial('SyncIndicator Pending Count', () => {
  let context: BrowserContext;
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    context = await browser.newContext();
    page = await context.newPage();
    await loginViaApi(page);
    await completeOnboarding();
  });

  test.afterAll(async () => {
    await context.close();
  });

  test('SyncIndicator shows pending count while offline', async () => {
    await page.goto('/board');
    await page.getByRole('navigation', { name: 'View switcher' }).waitFor({ state: 'visible' });

    // Go offline so the SyncIndicator's "pending" branch renders
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'onLine', { value: false, writable: true, configurable: true });
      window.dispatchEvent(new Event('offline'));
    });
    await page.waitForTimeout(500);

    // Enqueue a mutation directly into IndexedDB and refresh the Zustand store count.
    // This simulates what the app does when a network request fails while offline.
    await page.evaluate(async () => {
      const dbName = 'lemondo-offline';
      const storeName = 'mutations';
      const db: IDBDatabase = await new Promise((resolve, reject) => {
        const req = indexedDB.open(dbName, 1);
        req.onupgradeneeded = () => {
          if (!req.result.objectStoreNames.contains(storeName)) {
            req.result.createObjectStore(storeName, { keyPath: 'id' });
          }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
      });

      const mutation = {
        id: crypto.randomUUID(),
        timestamp: Date.now(),
        method: 'POST',
        url: '/api/tasks',
        body: JSON.stringify({ title: 'Sync indicator task' }),
      };

      const tx = db.transaction(storeName, 'readwrite');
      tx.objectStore(storeName).add(mutation);
      await new Promise<void>((resolve, reject) => {
        tx.oncomplete = () => resolve();
        tx.onerror = () => reject(tx.error);
      });
      db.close();
    });

    // Trigger the store to refresh its pendingCount from IndexedDB.
    // The store exposes refreshCount() which reads the IndexedDB queue count.
    // We dispatch a storage event as a workaround to trigger a React re-render,
    // then directly call refreshCount via the Zustand store's getState().
    await page.evaluate(async () => {
      // The useOfflineQueueStore is exposed on the module scope — we access it via import
      // Since we can't import modules in evaluate, we use the store's subscribe pattern.
      // The simplest way is to dispatch events that trigger the store's listeners.
      // However, the store's refreshCount is only called on init and not on events.
      // We need to trigger it manually by accessing the IndexedDB count
      // and forcing a Zustand state update.

      // Workaround: dispatch the 'online' event briefly to trigger drain which calls refreshCount,
      // then immediately go back offline to keep the pending state visible.
      // Instead, let's directly read the count and update the UI.

      // Access the store through the window — it's a module-scoped singleton
      const dbName = 'lemondo-offline';
      const storeName = 'mutations';
      const db: IDBDatabase = await new Promise((resolve, reject) => {
        const req = indexedDB.open(dbName, 1);
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
      });
      const tx = db.transaction(storeName, 'readonly');
      const countReq = tx.objectStore(storeName).count();
      const count = await new Promise<number>((resolve, reject) => {
        countReq.onsuccess = () => resolve(countReq.result);
        countReq.onerror = () => reject(countReq.error);
      });
      db.close();
      return count;
    });

    // The Zustand store's refreshCount needs to be called to update React state.
    // We can't access the store directly from page.evaluate, but we can trigger it
    // by briefly going online (which fires drain, which calls refreshCount) then offline again.
    // However, drain will also replay the mutation. Instead, let's abort the drain requests.
    await page.route('**/api/**', (route) => {
      // Block all API calls during the brief online blip
      route.abort('failed');
    });

    // Brief online blip to trigger drain -> refreshCount
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'onLine', { value: true, writable: true, configurable: true });
      window.dispatchEvent(new Event('online'));
    });
    await page.waitForTimeout(1000);

    // Go offline again so the SyncIndicator shows pending state
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'onLine', { value: false, writable: true, configurable: true });
      window.dispatchEvent(new Event('offline'));
    });
    await page.waitForTimeout(500);

    // Remove route interception
    await page.unroute('**/api/**');

    // The SyncIndicator (role="status") should display the pending count.
    // It shows "1 change pending" when there is 1 queued mutation while offline.
    const syncStatus = page.getByRole('status');
    await expect(syncStatus).toBeVisible({ timeout: 5000 });
    await expect(syncStatus).toContainText('pending');

    // Clean up: go back online so the mutation drains
    await page.evaluate(() => {
      Object.defineProperty(navigator, 'onLine', { value: true, writable: true, configurable: true });
      window.dispatchEvent(new Event('online'));
    });
    await page.waitForTimeout(5000);
  });
});
