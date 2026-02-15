import { useEffect, type ReactNode } from 'react';
import { useThemeStore, applyThemeClass } from '@/stores/use-theme-store';

interface ThemeProviderProps {
  children: ReactNode;
}

/**
 * Applies the persisted theme class on mount and listens for OS-level
 * color scheme changes when the user has selected "system" mode.
 */
export function ThemeProvider({ children }: ThemeProviderProps) {
  const theme = useThemeStore((s) => s.theme);

  useEffect(() => {
    applyThemeClass(theme);
  }, [theme]);

  // Listen for OS color scheme changes when in system mode
  useEffect(() => {
    if (theme !== 'system') return;

    const mq = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = () => applyThemeClass('system');
    mq.addEventListener('change', handler);
    return () => mq.removeEventListener('change', handler);
  }, [theme]);

  return <>{children}</>;
}
