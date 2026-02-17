import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useVisualViewport } from './use-visual-viewport';

describe('useVisualViewport', () => {
  let resizeListeners: (() => void)[];
  let mockHeight: number;
  const originalVisualViewport = window.visualViewport;

  beforeEach(() => {
    resizeListeners = [];
    mockHeight = 800;

    Object.defineProperty(window, 'visualViewport', {
      writable: true,
      configurable: true,
      value: {
        get height() {
          return mockHeight;
        },
        offsetTop: 0,
        addEventListener: (event: string, fn: () => void) => {
          if (event === 'resize') resizeListeners.push(fn);
        },
        removeEventListener: (event: string, fn: () => void) => {
          if (event === 'resize') {
            resizeListeners = resizeListeners.filter((l) => l !== fn);
          }
        },
      },
    });
  });

  afterEach(() => {
    document.documentElement.style.removeProperty('--visual-viewport-height');
    Object.defineProperty(window, 'visualViewport', {
      writable: true,
      configurable: true,
      value: originalVisualViewport,
    });
  });

  it('should return the current visual viewport height', () => {
    const { result } = renderHook(() => useVisualViewport());
    expect(result.current.height).toBe(800);
  });

  it('should update height when visual viewport fires resize event', () => {
    const { result } = renderHook(() => useVisualViewport());

    act(() => {
      mockHeight = 400;
      for (const fn of resizeListeners) fn();
    });

    expect(result.current.height).toBe(400);
  });

  it('should set --visual-viewport-height CSS custom property on documentElement', () => {
    renderHook(() => useVisualViewport());
    expect(
      document.documentElement.style.getPropertyValue('--visual-viewport-height'),
    ).toBe('800px');
  });

  it('should update CSS custom property when viewport height changes', () => {
    renderHook(() => useVisualViewport());

    act(() => {
      mockHeight = 350;
      for (const fn of resizeListeners) fn();
    });

    expect(
      document.documentElement.style.getPropertyValue('--visual-viewport-height'),
    ).toBe('350px');
  });

  it('should clean up event listeners on unmount', () => {
    const { unmount } = renderHook(() => useVisualViewport());
    expect(resizeListeners.length).toBeGreaterThan(0);

    unmount();
    expect(resizeListeners).toHaveLength(0);
  });

  it('should fall back to window.innerHeight when visualViewport is unavailable', () => {
    Object.defineProperty(window, 'visualViewport', {
      writable: true,
      configurable: true,
      value: null,
    });
    Object.defineProperty(window, 'innerHeight', {
      writable: true,
      configurable: true,
      value: 768,
    });

    const { result } = renderHook(() => useVisualViewport());
    expect(result.current.height).toBe(768);
  });
});
