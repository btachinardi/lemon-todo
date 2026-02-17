/**
 * Headless API helpers for verifying notification state in E2E tests.
 *
 * Use these for server-side assertions after browser interactions that
 * trigger notifications (e.g. verifying a welcome notification was created
 * after registration). All functions require an active auth session via
 * {@link createTestUser} or {@link loginViaApi}.
 *
 * Every function throws on non-2xx API responses.
 *
 * @module
 */

import { getAuthToken } from './auth.helpers';
import { API_BASE } from './e2e.config';

interface NotificationResponse {
  id: string;
  /** Notification type key (e.g. `'Welcome'`, `'TaskAssigned'`). Useful for filtering. */
  type: string;
  title: string;
  body: string | null;
  isRead: boolean;
  /** ISO 8601 timestamp when marked read, or `null` if unread. */
  readAt: string | null;
  /** ISO 8601 timestamp when the notification was created. */
  createdAt: string;
}

interface NotificationListResponse {
  items: NotificationResponse[];
  totalCount: number;
  page: number;
  pageSize: number;
}

interface UnreadCountResponse {
  count: number;
}

async function authHeaders(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  };
}

/**
 * Fetches the current user's notifications from the server. Use for
 * asserting that a notification was created after a browser action.
 *
 * @param page - Page number (1-indexed).
 * @param pageSize - Number of items per page.
 * @throws If the API returns a non-2xx response.
 *
 * @example
 * const { items } = await listNotifications();
 * const welcome = items.find(n => n.type === 'Welcome');
 * expect(welcome).toBeDefined();
 */
export async function listNotifications(page = 1, pageSize = 20): Promise<NotificationListResponse> {
  const res = await fetch(`${API_BASE}/notifications?page=${page}&pageSize=${pageSize}`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`List notifications failed: ${res.status}`);
  return res.json();
}

/**
 * Returns the number of unread notifications for the current user.
 * Use for asserting badge counts without fetching full notification data.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function getUnreadCount(): Promise<UnreadCountResponse> {
  const res = await fetch(`${API_BASE}/notifications/unread-count`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Get unread count failed: ${res.status}`);
  return res.json();
}

/**
 * Marks a single notification as read via API. Use when seeding read-state
 * for tests that verify "already read" UI rendering.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function markNotificationRead(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/notifications/${id}/read`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Mark notification read failed: ${res.status}`);
}

/**
 * Marks **all** notifications as read for the current user. This is
 * irreversible within the test run — subsequent assertions on unread
 * state will see zero unread items.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function markAllNotificationsRead(): Promise<void> {
  const res = await fetch(`${API_BASE}/notifications/read-all`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Mark all read failed: ${res.status}`);
}
