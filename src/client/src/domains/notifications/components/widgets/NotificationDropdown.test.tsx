import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { NotificationDropdown } from './NotificationDropdown';

const mockList = vi.fn();
const mockGetUnreadCount = vi.fn();
const mockMarkAsRead = vi.fn();
const mockMarkAllAsRead = vi.fn();

vi.mock('../../api/notifications.api', () => ({
  notificationsApi: {
    get list() { return mockList; },
    get getUnreadCount() { return mockGetUnreadCount; },
    get markAsRead() { return mockMarkAsRead; },
    get markAllAsRead() { return mockMarkAllAsRead; },
  },
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

describe('NotificationDropdown', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockMarkAsRead.mockResolvedValue(undefined);
    mockMarkAllAsRead.mockResolvedValue(undefined);
  });

  it('should show badge when there are unread notifications', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 3 });
    mockList.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    expect(await screen.findByText('3')).toBeInTheDocument();
  });

  it('should not show badge when there are no unread notifications', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 0 });
    mockList.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    // Wait for queries to settle
    await vi.waitFor(() => {
      expect(screen.queryByText('0')).not.toBeInTheDocument();
    });
  });

  it('should show notification list when bell is clicked', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 1 });
    mockList.mockResolvedValue({
      items: [
        {
          id: '1',
          type: 'Welcome',
          title: 'Welcome to Lemon.DO!',
          body: 'Start by creating your first task.',
          isRead: false,
          readAt: null,
          createdAt: new Date().toISOString(),
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    const user = userEvent.setup();
    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    // Click the bell
    const bell = await screen.findByRole('button', { name: /notifications/i });
    await user.click(bell);

    // Should show the notification
    expect(await screen.findByText('Welcome to Lemon.DO!')).toBeInTheDocument();
  });

  it('should show empty state when no notifications exist', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 0 });
    mockList.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    const user = userEvent.setup();
    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    const bell = await screen.findByRole('button', { name: /notifications/i });
    await user.click(bell);

    expect(await screen.findByText('No notifications yet')).toBeInTheDocument();
  });

  it('should show 9+ when unread count exceeds 9', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 15 });
    mockList.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    expect(await screen.findByText('9+')).toBeInTheDocument();
  });

  it('should call markAsRead with notification id when clicking unread notification', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 1 });
    mockList.mockResolvedValue({
      items: [
        {
          id: 'notif-42',
          type: 'Welcome',
          title: 'Hello there!',
          body: null,
          isRead: false,
          readAt: null,
          createdAt: new Date().toISOString(),
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 20,
    });

    const user = userEvent.setup();
    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    // Open the dropdown
    const bell = await screen.findByRole('button', { name: /notifications/i });
    await user.click(bell);

    // Click the unread notification
    const notification = await screen.findByText('Hello there!');
    await user.click(notification);

    expect(mockMarkAsRead).toHaveBeenCalledWith('notif-42', expect.anything());
  });

  it('should show visible label text when showLabel is true', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 0 });
    mockList.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    render(createElement(NotificationDropdown, { showLabel: true }), { wrapper: createWrapper() });

    // The label "Notifications" should be visible (not sr-only)
    expect(await screen.findByText('Notifications')).toBeVisible();
  });

  it('should not show visible label text by default', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 0 });
    mockList.mockResolvedValue({ items: [], totalCount: 0, page: 1, pageSize: 20 });

    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    // Wait for queries to settle, then verify no visible "Notifications" text
    await vi.waitFor(() => {
      expect(screen.queryByText('Notifications')).not.toBeInTheDocument();
    });
  });

  it('should call markAllAsRead when clicking mark all read button', async () => {
    mockGetUnreadCount.mockResolvedValue({ count: 2 });
    mockList.mockResolvedValue({
      items: [
        {
          id: 'n1',
          type: 'Welcome',
          title: 'First notification',
          body: null,
          isRead: false,
          readAt: null,
          createdAt: new Date().toISOString(),
        },
        {
          id: 'n2',
          type: 'DueDateReminder',
          title: 'Second notification',
          body: null,
          isRead: false,
          readAt: null,
          createdAt: new Date().toISOString(),
        },
      ],
      totalCount: 2,
      page: 1,
      pageSize: 20,
    });

    const user = userEvent.setup();
    render(createElement(NotificationDropdown), { wrapper: createWrapper() });

    // Open the dropdown
    const bell = await screen.findByRole('button', { name: /notifications/i });
    await user.click(bell);

    // Click the "Mark all read" button
    const markAllBtn = await screen.findByRole('button', { name: /mark all read/i });
    await user.click(markAllBtn);

    expect(mockMarkAllAsRead).toHaveBeenCalled();
  });
});
