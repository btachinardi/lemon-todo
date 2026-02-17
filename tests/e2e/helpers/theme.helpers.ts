import type { Page } from '@playwright/test';

/** Resolved (non-system) theme value for test injection. */
export type ResolvedTheme = 'light' | 'dark';

/** localStorage payload matching the Zustand persist shape for the theme store. */
export function themePayload(theme: ResolvedTheme): string {
  return JSON.stringify({ state: { theme }, version: 0 });
}

/**
 * Configures a page to use the given theme before any navigation.
 *
 * Injects localStorage via `addInitScript` so the Zustand persist
 * middleware rehydrates with the correct theme on page load.
 */
export function setThemeBeforeLoad(page: Page, theme: ResolvedTheme): void {
  const payload = themePayload(theme);
  void page.addInitScript(
    ({ key, value, cls }: { key: string; value: string; cls: string }) => {
      localStorage.setItem(key, value);
      document.documentElement.classList.remove('light', 'dark');
      document.documentElement.classList.add(cls);
    },
    { key: 'lemondo-theme', value: payload, cls: theme },
  );
}

/**
 * Ensures the theme class is applied and stable after navigation.
 *
 * The Zustand persist store initializes with `dark` (default) before
 * async rehydration from localStorage. This causes a brief flash where
 * ThemeProvider applies 'dark' before rehydration restores the correct
 * theme. This helper forces the class after the page is interactive
 * and verifies it sticks.
 */
export async function waitForTheme(page: Page, theme: ResolvedTheme): Promise<void> {
  const alreadyCorrect = await page.evaluate(
    (cls: string) => document.documentElement.classList.contains(cls),
    theme,
  );
  if (alreadyCorrect) return;

  const payload = themePayload(theme);
  await page.evaluate(
    ({ key, value, cls }: { key: string; value: string; cls: string }) => {
      localStorage.setItem(key, value);
      document.documentElement.classList.remove('light', 'dark');
      document.documentElement.classList.add(cls);
    },
    { key: 'lemondo-theme', value: payload, cls: theme },
  );

  // Let the override cascade and browser reflow
  await page.waitForTimeout(300);

  // Verify the class is stable (React re-render didn't override it)
  await page.waitForFunction(
    (cls: string) => document.documentElement.classList.contains(cls),
    theme,
    { timeout: 5_000 },
  );
}
