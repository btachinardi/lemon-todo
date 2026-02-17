import { useEffect, useSyncExternalStore } from 'react';

function subscribe(callback: () => void) {
  const vv = window.visualViewport;
  if (!vv) return () => {};
  vv.addEventListener('resize', callback);
  return () => vv.removeEventListener('resize', callback);
}

function getSnapshot() {
  return window.visualViewport?.height ?? window.innerHeight;
}

function getServerSnapshot() {
  return 0;
}

/**
 * Tracks the visual viewport height and sets a `--visual-viewport-height` CSS
 * custom property on `<html>`. This enables dialogs and sheets to constrain
 * their height to the visible area when the mobile virtual keyboard is open.
 */
export function useVisualViewport(): { height: number } {
  const height = useSyncExternalStore(subscribe, getSnapshot, getServerSnapshot);

  useEffect(() => {
    if (height > 0) {
      document.documentElement.style.setProperty(
        '--visual-viewport-height',
        `${height}px`,
      );
    }
  }, [height]);

  return { height };
}
