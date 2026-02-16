import { useTranslation } from 'react-i18next';
import { BellIcon, CalendarClockIcon, AlertTriangleIcon, SparklesIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
import type { Notification } from '../../types/notification.types';

interface NotificationItemProps {
  notification: Notification;
  onMarkAsRead: (id: string) => void;
}

const typeIcons: Record<string, typeof BellIcon> = {
  DueDateReminder: CalendarClockIcon,
  TaskOverdue: AlertTriangleIcon,
  Welcome: SparklesIcon,
};

/** Single notification row in the dropdown. */
export function NotificationItem({ notification, onMarkAsRead }: NotificationItemProps) {
  const { t } = useTranslation();
  const Icon = typeIcons[notification.type] ?? BellIcon;

  const timeAgo = getRelativeTime(notification.createdAt, t);

  return (
    <button
      className={cn(
        'flex w-full items-start gap-3 rounded-lg px-3 py-2.5 text-left transition-colors hover:bg-secondary/60',
        !notification.isRead && 'bg-primary/5',
      )}
      onClick={() => !notification.isRead && onMarkAsRead(notification.id)}
    >
      <div className={cn(
        'mt-0.5 rounded-full p-1.5',
        !notification.isRead ? 'bg-primary/10 text-primary' : 'bg-muted text-muted-foreground',
      )}>
        <Icon className="size-3.5" />
      </div>
      <div className="min-w-0 flex-1">
        <p className={cn('text-sm leading-tight', !notification.isRead && 'font-semibold')}>
          {notification.title}
        </p>
        {notification.body && (
          <p className="mt-0.5 text-xs text-muted-foreground line-clamp-2">{notification.body}</p>
        )}
        <p className="mt-1 text-[11px] text-muted-foreground/70">{timeAgo}</p>
      </div>
      {!notification.isRead && (
        <div className="mt-2 size-2 shrink-0 rounded-full bg-primary" />
      )}
    </button>
  );
}

function getRelativeTime(dateStr: string, _t: ReturnType<typeof useTranslation>['t']): string {
  const date = new Date(dateStr);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMins = Math.floor(diffMs / 60_000);

  if (diffMins < 1) return 'Just now';
  if (diffMins < 60) return `${diffMins}m ago`;
  const diffHours = Math.floor(diffMins / 60);
  if (diffHours < 24) return `${diffHours}h ago`;
  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) return `${diffDays}d ago`;
  return date.toLocaleDateString();
}
