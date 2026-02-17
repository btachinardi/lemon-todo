import type { components } from '../../../api/schema';

/** Notification type discriminator, derived from the backend enum via OpenAPI schema. */
export type NotificationType = components['schemas']['NotificationResponse']['type'];

/** A single notification from the API. */
export type Notification = components['schemas']['NotificationResponse'];

/** Paginated list of notifications. */
export interface NotificationListResponse {
  items: Notification[];
  totalCount: number;
  page: number;
  pageSize: number;
}

/** Count of unread notifications. */
export interface UnreadCountResponse {
  count: number;
}
