import { registerSW } from 'virtual:pwa-register';

let deferredPrompt: BeforeInstallPromptEvent | null = null;
let updateSW: ((reloadPage?: boolean) => Promise<void>) | null = null;

const installListeners = new Set<(available: boolean) => void>();
const updateListeners = new Set<(available: boolean) => void>();

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

/**
 * Register the service worker and listen for install/update events.
 * Call once at app startup.
 */
export function initPWA(): void {
  updateSW = registerSW({
    onNeedRefresh() {
      updateListeners.forEach((cb) => cb(true));
    },
    onOfflineReady() {
      // App is ready for offline use — no action needed
    },
  });

  window.addEventListener('beforeinstallprompt', (e) => {
    e.preventDefault();
    deferredPrompt = e as BeforeInstallPromptEvent;
    installListeners.forEach((cb) => cb(true));
  });

  window.addEventListener('appinstalled', () => {
    deferredPrompt = null;
    installListeners.forEach((cb) => cb(false));
  });
}

/**
 * Subscribe to install prompt availability changes.
 */
export function onInstallAvailable(cb: (available: boolean) => void): () => void {
  installListeners.add(cb);
  return () => installListeners.delete(cb);
}

/**
 * Returns true if the PWA install prompt is available.
 */
export function isInstallAvailable(): boolean {
  return deferredPrompt !== null;
}

/**
 * Trigger the PWA install prompt. Returns true if the user accepted.
 */
export async function promptInstall(): Promise<boolean> {
  if (!deferredPrompt) return false;
  await deferredPrompt.prompt();
  const { outcome } = await deferredPrompt.userChoice;
  deferredPrompt = null;
  installListeners.forEach((cb) => cb(false));
  return outcome === 'accepted';
}

/**
 * Subscribe to update-available events.
 */
export function onUpdateAvailable(cb: (available: boolean) => void): () => void {
  updateListeners.add(cb);
  return () => updateListeners.delete(cb);
}

/**
 * Reload the page to apply the pending update.
 */
export async function applyUpdate(): Promise<void> {
  if (updateSW) {
    await updateSW(true);
  }
}
