import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { BellIcon, CheckCheckIcon, LoaderIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/ui/popover';
import { useNotifications, useUnreadCount, useMarkAsRead, useMarkAllAsRead } from '../../hooks/use-notifications';
import { NotificationItem } from '../atoms/NotificationItem';

interface NotificationDropdownProps {
  /** When true, renders a visible text label next to the bell icon. */
  showLabel?: boolean;
}

/**
 * Notification bell with badge count and dropdown list.
 * Polls for unread count every 30s. Shows up to 20 recent notifications.
 */
export function NotificationDropdown({ showLabel }: NotificationDropdownProps = {}) {
  const { t } = useTranslation();
  const [open, setOpen] = useState(false);

  const { data: unreadData } = useUnreadCount();
  const { data: notificationData, isLoading } = useNotifications(1);
  const markAsRead = useMarkAsRead();
  const markAllAsRead = useMarkAllAsRead();

  const unreadCount = unreadData?.count ?? 0;
  const notifications = notificationData?.items ?? [];

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className={showLabel ? 'relative justify-start gap-2 px-3 py-2' : 'relative p-2'}
          aria-label={t('notifications.bell', { count: unreadCount })}
        >
          <BellIcon className="size-4" />
          {showLabel && <span className="text-base">{t('notifications.title')}</span>}
          {unreadCount > 0 && (
            <span className={showLabel
              ? 'ml-auto flex size-4 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-primary-foreground'
              : 'absolute -right-0.5 -top-0.5 flex size-4 items-center justify-center rounded-full bg-primary text-[10px] font-bold text-primary-foreground'
            }>
              {unreadCount > 9 ? '9+' : unreadCount}
            </span>
          )}
        </Button>
      </PopoverTrigger>
      <PopoverContent
        side="bottom"
        align="end"
        className="w-80 p-0 sm:w-96"
      >
        <div className="flex items-center justify-between border-b border-border/50 px-4 py-3">
          <h3 className="text-base font-semibold">{t('notifications.title')}</h3>
          {unreadCount > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="h-auto gap-1 px-2 py-1 text-sm"
              onClick={() => markAllAsRead.mutate()}
              disabled={markAllAsRead.isPending}
            >
              {markAllAsRead.isPending ? (
                <LoaderIcon className="size-3 animate-spin" />
              ) : (
                <CheckCheckIcon className="size-3" />
              )}
              {t('notifications.markAllRead')}
            </Button>
          )}
        </div>
        <div className="max-h-80 overflow-y-auto p-1">
          {isLoading ? (
            <div className="flex items-center justify-center py-8">
              <LoaderIcon className="size-5 animate-spin text-muted-foreground" />
            </div>
          ) : notifications.length === 0 ? (
            <div className="py-8 text-center text-base text-muted-foreground">
              {t('notifications.empty')}
            </div>
          ) : (
            notifications.map((n) => (
              <NotificationItem
                key={n.id}
                notification={n}
                onMarkAsRead={(id) => markAsRead.mutate(id)}
              />
            ))
          )}
        </div>
      </PopoverContent>
    </Popover>
  );
}
