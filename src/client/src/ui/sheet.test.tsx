import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import {
  Sheet,
  SheetContent,
  SheetTitle,
  SheetDescription,
} from './sheet';

vi.mock('@/hooks/use-visual-viewport', () => ({
  useVisualViewport: () => ({ height: 500 }),
}));

function renderSheet(
  children?: React.ReactNode,
  side: 'right' | 'left' | 'top' | 'bottom' = 'right',
) {
  return render(
    <Sheet open>
      <SheetContent side={side}>
        <SheetTitle>Test Sheet</SheetTitle>
        <SheetDescription>A test sheet</SheetDescription>
        {children}
      </SheetContent>
    </Sheet>,
  );
}

describe('SheetContent — keyboard-aware behavior', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should apply overflow-y-auto so content scrolls when height-constrained', () => {
    renderSheet();
    const dialog = screen.getByRole('dialog');
    expect(dialog.className).toContain('overflow-y-auto');
  });

  it('should constrain max-height to visual viewport height for right-side sheet', () => {
    renderSheet(undefined, 'right');
    const dialog = screen.getByRole('dialog');
    expect(dialog.style.maxHeight).toBe('500px');
  });

  it('should constrain max-height to visual viewport height for left-side sheet', () => {
    renderSheet(undefined, 'left');
    const dialog = screen.getByRole('dialog');
    expect(dialog.style.maxHeight).toBe('500px');
  });

  it('should scroll focused input into view after a short delay', () => {
    const scrollIntoViewMock = vi.fn();
    renderSheet(<input data-testid="sheet-input" />);

    const input = screen.getByTestId('sheet-input');
    input.scrollIntoView = scrollIntoViewMock;

    fireEvent.focus(input);
    act(() => {
      vi.advanceTimersByTime(150);
    });

    expect(scrollIntoViewMock).toHaveBeenCalledWith(
      expect.objectContaining({ block: 'nearest' }),
    );
  });

  it('should scroll focused textarea into view after a short delay', () => {
    const scrollIntoViewMock = vi.fn();
    renderSheet(<textarea data-testid="sheet-textarea" />);

    const textarea = screen.getByTestId('sheet-textarea');
    textarea.scrollIntoView = scrollIntoViewMock;

    fireEvent.focus(textarea);
    act(() => {
      vi.advanceTimersByTime(150);
    });

    expect(scrollIntoViewMock).toHaveBeenCalledWith(
      expect.objectContaining({ block: 'nearest' }),
    );
  });

  it('should not scroll non-input elements into view on focus', () => {
    const scrollIntoViewMock = vi.fn();
    renderSheet(<button data-testid="sheet-btn">Click</button>);

    const btn = screen.getByTestId('sheet-btn');
    btn.scrollIntoView = scrollIntoViewMock;

    fireEvent.focus(btn);
    act(() => {
      vi.advanceTimersByTime(150);
    });

    expect(scrollIntoViewMock).not.toHaveBeenCalled();
  });
});
