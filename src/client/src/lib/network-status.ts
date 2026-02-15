import { captureWarning, captureInfo } from './error-logger';

type NetworkCallback = (isOnline: boolean) => void;

const listeners = new Set<NetworkCallback>();

/**
 * Subscribe to network status changes. Returns an unsubscribe function.
 */
export function onNetworkChange(callback: NetworkCallback): () => void {
  listeners.add(callback);
  return () => listeners.delete(callback);
}

/**
 * Returns the current online/offline status.
 */
export function getNetworkStatus(): boolean {
  return navigator.onLine;
}

function handleOnline(): void {
  captureInfo('Network connection restored', { source: 'NetworkStatus' });
  listeners.forEach((cb) => cb(true));
}

function handleOffline(): void {
  captureWarning('Network connection lost', { source: 'NetworkStatus' });
  listeners.forEach((cb) => cb(false));
}

/**
 * Initializes network status monitoring. Call once at app startup.
 */
export function initNetworkMonitoring(): void {
  window.addEventListener('online', handleOnline);
  window.addEventListener('offline', handleOffline);
}
