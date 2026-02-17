import { getAuthToken } from './auth.helpers';
import { API_BASE } from './e2e.config';

interface NotificationResponse {
  id: string;
  type: string;
  title: string;
  body: string | null;
  isRead: boolean;
  readAt: string | null;
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

/** Builds request headers with auth token. */
async function authHeaders(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  };
}

/** Fetches the current user's notifications (paginated). */
export async function listNotifications(page = 1, pageSize = 20): Promise<NotificationListResponse> {
  const res = await fetch(`${API_BASE}/notifications?page=${page}&pageSize=${pageSize}`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`List notifications failed: ${res.status}`);
  return res.json();
}

/** Gets the current user's unread notification count. */
export async function getUnreadCount(): Promise<UnreadCountResponse> {
  const res = await fetch(`${API_BASE}/notifications/unread-count`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Get unread count failed: ${res.status}`);
  return res.json();
}

/** Marks a single notification as read. */
export async function markNotificationRead(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/notifications/${id}/read`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Mark notification read failed: ${res.status}`);
}

/** Marks all notifications as read. */
export async function markAllNotificationsRead(): Promise<void> {
  const res = await fetch(`${API_BASE}/notifications/read-all`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Mark all read failed: ${res.status}`);
}
