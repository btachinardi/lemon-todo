import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, act } from '@testing-library/react';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  DialogDescription,
} from './dialog';

vi.mock('@/hooks/use-visual-viewport', () => ({
  useVisualViewport: () => ({ height: 500 }),
}));

function renderDialog(children?: React.ReactNode) {
  return render(
    <Dialog open>
      <DialogContent>
        <DialogTitle>Test Dialog</DialogTitle>
        <DialogDescription>A test dialog</DialogDescription>
        {children}
      </DialogContent>
    </Dialog>,
  );
}

describe('DialogContent — keyboard-aware behavior', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should apply overflow-y-auto so content scrolls when height-constrained', () => {
    renderDialog();
    const dialog = screen.getByRole('dialog');
    expect(dialog.className).toContain('overflow-y-auto');
  });

  it('should constrain max-height to visual viewport height minus padding', () => {
    renderDialog();
    const dialog = screen.getByRole('dialog');
    // Hook returns 500, so max-height should be 500 - 32 (1rem top + 1rem bottom) = 468px
    expect(dialog.style.maxHeight).toBe('468px');
  });

  it('should scroll focused input into view after a short delay', () => {
    const scrollIntoViewMock = vi.fn();
    renderDialog(<input data-testid="email-input" />);

    const input = screen.getByTestId('email-input');
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
    renderDialog(<textarea data-testid="notes" />);

    const textarea = screen.getByTestId('notes');
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
    renderDialog(<button data-testid="action-btn">Click</button>);

    const btn = screen.getByTestId('action-btn');
    btn.scrollIntoView = scrollIntoViewMock;

    fireEvent.focus(btn);
    act(() => {
      vi.advanceTimersByTime(150);
    });

    expect(scrollIntoViewMock).not.toHaveBeenCalled();
  });
});
