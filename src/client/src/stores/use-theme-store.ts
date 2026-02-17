import { create } from 'zustand';
import { persist } from 'zustand/middleware';

/** User theme preference: explicit light/dark or system (follows OS setting). */
export type Theme = 'light' | 'dark' | 'system';

interface ThemeState {
  theme: Theme;
  setTheme: (theme: Theme) => void;
}

/**
 * Resolves 'system' to the actual OS preference, otherwise returns the explicit theme.
 *
 * @param theme - The user's theme preference.
 * @returns The resolved theme ('light' or 'dark'). Queries OS preference if theme is 'system'.
 */
export function resolveTheme(theme: Theme): 'light' | 'dark' {
  if (theme !== 'system') return theme;
  if (typeof window === 'undefined') return 'dark';
  return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

/** Background hex colors matching CSS --background for each theme. */
const themeBackground: Record<'light' | 'dark', string> = {
  dark: '#000000',
  light: '#f3f3f3',
};

/**
 * Applies the resolved theme class to the document root element and updates
 * the PWA theme-color meta tag to match the background.
 * Side effect: Mutates document.documentElement.classList and meta[name="theme-color"].
 *
 * @param theme - The user's theme preference to apply.
 */
export function applyThemeClass(theme: Theme): void {
  const resolved = resolveTheme(theme);
  const root = document.documentElement;
  root.classList.remove('light', 'dark');
  root.classList.add(resolved);

  const meta = document.querySelector('meta[name="theme-color"]');
  if (meta) meta.setAttribute('content', themeBackground[resolved]);
}

/** Zustand store for theme preference. Persisted to localStorage. */
export const useThemeStore = create<ThemeState>()(
  persist(
    (set) => ({
      theme: 'dark',
      setTheme: (theme) => {
        applyThemeClass(theme);
        set({ theme });
      },
    }),
    {
      name: 'lemondo-theme',
      onRehydrateStorage: () => (state) => {
        if (state) {
          applyThemeClass(state.theme);
        }
      },
    },
  ),
);
