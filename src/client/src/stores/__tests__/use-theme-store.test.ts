import { describe, it, expect, beforeEach, vi } from 'vitest';

// Zustand persist requires a full Storage implementation.
// Must run before the store module is imported (vi.hoisted runs before ES import hoisting).
vi.hoisted(() => {
  const store: Record<string, string> = {};
  Object.defineProperty(globalThis, 'localStorage', {
    value: {
      getItem: (key: string) => store[key] ?? null,
      setItem: (key: string, value: string) => { store[key] = value; },
      removeItem: (key: string) => { delete store[key]; },
      clear: () => { for (const k in store) delete store[k]; },
      get length() { return Object.keys(store).length; },
      key: (index: number) => Object.keys(store)[index] ?? null,
    },
    writable: true,
    configurable: true,
  });
});

import { resolveTheme, applyThemeClass, useThemeStore } from '../use-theme-store';

// Mock matchMedia for jsdom
function mockMatchMedia(matches: boolean) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation((query: string) => ({
      matches,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    })),
  });
}

describe('use-theme-store', () => {
  beforeEach(() => {
    // Reset store state
    useThemeStore.setState({ theme: 'dark' });
    // Clean up document classes
    document.documentElement.classList.remove('light', 'dark');
    // Default mock (light mode)
    mockMatchMedia(false);
  });

  describe('resolveTheme', () => {
    it('should return "dark" for explicit dark theme', () => {
      expect(resolveTheme('dark')).toBe('dark');
    });

    it('should return "light" for explicit light theme', () => {
      expect(resolveTheme('light')).toBe('light');
    });

    it('should resolve "system" based on matchMedia', () => {
      // jsdom matchMedia returns false by default (light mode)
      expect(resolveTheme('system')).toBe('light');
    });
  });

  describe('applyThemeClass', () => {
    it('should add "dark" class for dark theme', () => {
      applyThemeClass('dark');
      expect(document.documentElement.classList.contains('dark')).toBe(true);
      expect(document.documentElement.classList.contains('light')).toBe(false);
    });

    it('should add "light" class for light theme', () => {
      applyThemeClass('light');
      expect(document.documentElement.classList.contains('light')).toBe(true);
      expect(document.documentElement.classList.contains('dark')).toBe(false);
    });

    it('should remove previous class when switching', () => {
      applyThemeClass('dark');
      applyThemeClass('light');
      expect(document.documentElement.classList.contains('dark')).toBe(false);
      expect(document.documentElement.classList.contains('light')).toBe(true);
    });
  });

  describe('store', () => {
    it('should default to dark theme', () => {
      expect(useThemeStore.getState().theme).toBe('dark');
    });

    it('should update theme via setTheme', () => {
      useThemeStore.getState().setTheme('light');
      expect(useThemeStore.getState().theme).toBe('light');
    });
  });
});
