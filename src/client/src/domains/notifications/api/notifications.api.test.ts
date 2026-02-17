import { describe, it, expect, vi, beforeEach } from 'vitest';
import { notificationsApi } from './notifications.api';

const mockPostVoid = vi.fn();
const mockGet = vi.fn();

vi.mock('@/lib/api-client', () => ({
  apiClient: {
    get: (...args: unknown[]) => mockGet(...args),
    postVoid: (...args: unknown[]) => mockPostVoid(...args),
  },
}));

describe('notificationsApi', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockPostVoid.mockResolvedValue(undefined);
    mockGet.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });
  });

  describe('markAsRead', () => {
    it('should call postVoid with the correct URL', async () => {
      await notificationsApi.markAsRead('abc-123');

      expect(mockPostVoid).toHaveBeenCalledWith('/api/notifications/abc-123/read');
    });

    it('should not throw on void response', async () => {
      mockPostVoid.mockResolvedValue(undefined);

      await expect(notificationsApi.markAsRead('abc-123')).resolves.toBeUndefined();
    });
  });

  describe('markAllAsRead', () => {
    it('should call postVoid with the correct URL', async () => {
      await notificationsApi.markAllAsRead();

      expect(mockPostVoid).toHaveBeenCalledWith('/api/notifications/read-all');
    });

    it('should not throw on void response', async () => {
      mockPostVoid.mockResolvedValue(undefined);

      await expect(notificationsApi.markAllAsRead()).resolves.toBeUndefined();
    });
  });
});
