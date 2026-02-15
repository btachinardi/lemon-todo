import { useSyncExternalStore } from 'react';

/**
 * Returns true when the given CSS media query matches.
 * Uses useSyncExternalStore for glitch-free reads of matchMedia.
 */
export function useMediaQuery(query: string): boolean {
  return useSyncExternalStore(
    (callback) => {
      const mq = window.matchMedia(query);
      mq.addEventListener('change', callback);
      return () => mq.removeEventListener('change', callback);
    },
    () => window.matchMedia(query).matches,
    () => false, // server snapshot
  );
}
