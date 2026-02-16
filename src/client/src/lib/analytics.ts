import { API_BASE_URL } from './api-client';

interface AnalyticsEvent {
  eventName: string;
  properties?: Record<string, string>;
  timestamp: string;
}

const eventBuffer: AnalyticsEvent[] = [];
const FLUSH_INTERVAL_MS = 30_000;
let flushTimer: ReturnType<typeof setInterval> | null = null;

/**
 * Hashes a string using SHA-256 and returns the first 16 hex chars.
 * Used to anonymize user IDs before sending to analytics.
 */
async function hashString(value: string): Promise<string> {
  const data = new TextEncoder().encode(value);
  const hashBuffer = await crypto.subtle.digest('SHA-256', data);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray.map((b) => b.toString(16).padStart(2, '0')).join('').slice(0, 16);
}

/**
 * Captures device context for analytics events.
 */
function getDeviceContext(): Record<string, string> {
  return {
    viewport: `${window.innerWidth}x${window.innerHeight}`,
    locale: navigator.language,
    theme: document.documentElement.classList.contains('dark') ? 'dark' : 'light',
    appVersion: (globalThis as Record<string, unknown>).__APP_VERSION__ as string ?? 'unknown',
  };
}

/**
 * Flushes buffered events to the analytics endpoint.
 * Silently fails on network errors (analytics should never block UX).
 */
async function flush(): Promise<void> {
  if (eventBuffer.length === 0) return;

  const events = eventBuffer.splice(0);

  try {
    await fetch(`${API_BASE_URL}/api/analytics/events`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        events: events.map((e) => ({
          eventName: e.eventName,
          properties: e.properties,
        })),
      }),
      credentials: 'include',
      keepalive: true, // allows sending during page unload
    });
  } catch {
    // Analytics failures are silent — never block UX
  }
}

/**
 * Track an analytics event. Events are buffered and flushed in batches.
 * Never include task content (titles, descriptions) in properties.
 */
export async function track(
  eventName: string,
  properties?: Record<string, string>,
): Promise<void> {
  const deviceContext = getDeviceContext();
  const merged = { ...deviceContext, ...properties };

  eventBuffer.push({
    eventName,
    properties: merged,
    timestamp: new Date().toISOString(),
  });
}

/**
 * Track an event with a hashed user ID.
 */
export async function trackWithUser(
  eventName: string,
  userId: string,
  properties?: Record<string, string>,
): Promise<void> {
  const hashedId = await hashString(userId);
  await track(eventName, { ...properties, hashedUserId: hashedId });
}

/**
 * Initialize the analytics flush timer and visibility change listener.
 * Call once at app startup.
 */
export function initAnalytics(): void {
  flushTimer = setInterval(flush, FLUSH_INTERVAL_MS);

  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'hidden') {
      flush();
    }
  });
}

/**
 * Stop the analytics timer (for testing or cleanup).
 */
export function stopAnalytics(): void {
  if (flushTimer) {
    clearInterval(flushTimer);
    flushTimer = null;
  }
}

/**
 * Get the current number of buffered events (for testing).
 */
export function getBufferSize(): number {
  return eventBuffer.length;
}
