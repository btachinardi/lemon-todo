import { useNetworkStatus } from '@/hooks/use-network-status';

/**
 * Renders a fixed banner at the top of the viewport when the browser is offline.
 * Automatically hides when connectivity is restored.
 */
export function OfflineBanner() {
  const isOnline = useNetworkStatus();

  if (isOnline) return null;

  return (
    <div
      role="alert"
      className="fixed top-0 inset-x-0 z-50 bg-destructive text-destructive-foreground px-4 py-2 text-center text-sm font-medium"
    >
      You are offline. Changes will not be saved until your connection is restored.
    </div>
  );
}
