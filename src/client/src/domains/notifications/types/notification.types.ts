export interface Notification {
  id: string;
  type: string;
  title: string;
  body: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface NotificationListResponse {
  items: Notification[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface UnreadCountResponse {
  count: number;
}
