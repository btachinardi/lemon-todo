import { describe, it, expect, beforeEach, vi } from 'vitest';
import indexCss from '../../index.css?raw';

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

/**
 * Extract a CSS block by selector from raw CSS text.
 * Handles nested braces (e.g. @keyframes inside a block).
 */
function extractCssBlock(css: string, selector: string): string | null {
  const startIndex = css.indexOf(selector);
  if (startIndex === -1) return null;
  const braceStart = css.indexOf('{', startIndex);
  if (braceStart === -1) return null;
  let depth = 0;
  for (let i = braceStart; i < css.length; i++) {
    if (css[i] === '{') depth++;
    if (css[i] === '}') depth--;
    if (depth === 0) return css.slice(braceStart + 1, i);
  }
  return null;
}

/**
 * Extract a CSS custom property value from a CSS block string.
 */
function extractCssVar(block: string, varName: string): string | null {
  const regex = new RegExp(`${varName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}:\\s*([^;]+);`);
  const match = block.match(regex);
  return match ? match[1].trim() : null;
}

describe('theme design tokens (index.css)', () => {
  const lightBlock = extractCssBlock(indexCss, '.light');
  const darkBlock = extractCssBlock(indexCss, ':root');

  it('should have a .light block in index.css', () => {
    expect(lightBlock).not.toBeNull();
  });

  it('should have a :root (dark) block in index.css', () => {
    expect(darkBlock).not.toBeNull();
  });

  describe('--highlight in light theme', () => {
    it('should use purple #832ce0 (oklch hue ~303) instead of lime-green', () => {
      const highlightValue = extractCssVar(lightBlock!, '--highlight');
      expect(highlightValue).not.toBeNull();
      // Must NOT be the old lime-green value (hue 120)
      expect(highlightValue).not.toContain('120');
      // Must be in the purple hue range (~303 in oklch)
      expect(highlightValue).toContain('303');
    });

    it('should not use the old lime-green value for --highlight', () => {
      const highlightValue = extractCssVar(lightBlock!, '--highlight');
      expect(highlightValue).not.toBe('oklch(0.52 0.22 120)');
    });
  });

  describe('--highlight in dark theme', () => {
    it('should keep the original lime-green value', () => {
      const highlightValue = extractCssVar(darkBlock!, '--highlight');
      expect(highlightValue).toBe('oklch(0.94 0.24 116)');
    });
  });
});
