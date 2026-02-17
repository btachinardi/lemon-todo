import { apiClient } from '@/lib/api-client';
import type { NotificationListResponse, UnreadCountResponse } from '../types/notification.types';

export const notificationsApi = {
  list: (page = 1, pageSize = 20) =>
    apiClient.get<NotificationListResponse>(`/api/notifications?page=${page}&pageSize=${pageSize}`),

  getUnreadCount: () =>
    apiClient.get<UnreadCountResponse>('/api/notifications/unread-count'),

  markAsRead: (id: string) =>
    apiClient.postVoid(`/api/notifications/${id}/read`),

  markAllAsRead: () =>
    apiClient.postVoid('/api/notifications/read-all'),
};
