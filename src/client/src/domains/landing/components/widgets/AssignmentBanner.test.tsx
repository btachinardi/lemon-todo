import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { createElement, type ReactNode } from 'react';
import { MemoryRouter } from 'react-router';

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'assignmentBanner.title': 'A Message to Evaluators',
        'assignmentBanner.greeting': "Hi, I'm Bruno.",
        'assignmentBanner.message': 'This is a take-home assignment.',
        'assignmentBanner.process': 'Production reasoning.',
        'assignmentBanner.branding': 'Branding is intentional.',
        'assignmentBanner.personal': 'I had a blast building this.',
        'assignmentBanner.ctaMethodology': 'View Full Methodology',
        'assignmentBanner.ctaPortfolio': 'Portfolio',
        'assignmentBanner.dismiss': 'Close',
      };
      return translations[key] ?? key;
    },
  }),
}));

// Mock motion/react – render standard HTML, bypass animations
vi.mock('motion/react', async () => {
  const React = await vi.importActual<typeof import('react')>('react');
  const MotionDiv = React.forwardRef(
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    ({ initial, animate, exit, transition, ...rest }: Record<string, unknown>, ref: unknown) =>
      React.createElement('div', { ...rest, ref }),
  );
  const MotionButton = React.forwardRef(
    // eslint-disable-next-line @typescript-eslint/no-unused-vars
    ({ initial, animate, exit, transition, ...rest }: Record<string, unknown>, ref: unknown) =>
      React.createElement('button', { ...rest, ref }),
  );
  return {
    motion: { div: MotionDiv, button: MotionButton },
    AnimatePresence: ({ children }: { children: ReactNode }) => children,
  };
});

// Ensure localStorage is available
const storage: Record<string, string> = {};
vi.hoisted(() => {
  if (typeof globalThis.localStorage === 'undefined' || typeof globalThis.localStorage.clear !== 'function') {
    Object.defineProperty(globalThis, 'localStorage', {
      value: {
        getItem: (key: string) => storage[key] ?? null,
        setItem: (key: string, value: string) => { storage[key] = value; },
        removeItem: (key: string) => { delete storage[key]; },
        clear: () => { for (const k in storage) delete storage[k]; },
        get length() { return Object.keys(storage).length; },
        key: (i: number) => Object.keys(storage)[i] ?? null,
      },
      writable: true,
      configurable: true,
    });
  }
});

import { AssignmentBanner } from './AssignmentBanner';

function renderWithRouter(ui: React.ReactElement) {
  return render(createElement(MemoryRouter, null, ui));
}

describe('AssignmentBanner', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    localStorage.clear();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  function advancePastBubbleAndModal() {
    // Bubble appears after 600ms, modal auto-opens 1200ms after that (first visit)
    act(() => { vi.advanceTimersByTime(600); });
    act(() => { vi.advanceTimersByTime(1200); });
  }

  describe('modal positioning', () => {
    it('should center the modal using inset + m-auto instead of bottom-anchoring', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      const modal = screen.getByRole('dialog');
      const classes = modal.className;

      // Must use inset-based centering (not bottom-anchored)
      expect(classes).toContain('inset-4');
      expect(classes).toContain('m-auto');

      // Must NOT have bottom-anchor positioning
      expect(classes).not.toContain('bottom-4');
      expect(classes).not.toContain('top-auto');
      expect(classes).not.toContain('inset-x-4');
    });

    it('should use dvh units for max-height to account for mobile browser chrome', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      const modal = screen.getByRole('dialog');
      expect(modal.className).toContain('dvh');
    });

    it('should not have breakpoint-specific centering transforms', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      const modal = screen.getByRole('dialog');
      const classes = modal.className;

      // Centering should work on all breakpoints, not just sm:
      expect(classes).not.toContain('sm:left-1/2');
      expect(classes).not.toContain('sm:top-1/2');
      expect(classes).not.toContain('sm:-translate-x-1/2');
      expect(classes).not.toContain('sm:-translate-y-1/2');
      expect(classes).not.toContain('sm:inset-auto');
    });
  });

  describe('modal scroll behavior', () => {
    it('should use flex column layout on the modal panel to separate header, body, and CTAs', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      const modal = screen.getByRole('dialog');
      const classes = modal.className;

      expect(classes).toContain('flex');
      expect(classes).toContain('flex-col');
    });

    it('should not have overflow-y-auto on the modal container itself', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      const modal = screen.getByRole('dialog');
      expect(modal.className).not.toContain('overflow-y-auto');
    });

    it('should have overflow-y-auto on the body content area only', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      // The body contains the message paragraphs
      const bodyText = screen.getByText('This is a take-home assignment.');
      const bodyContainer = bodyText.parentElement!;

      expect(bodyContainer.className).toContain('overflow-y-auto');
    });

    it('should have min-h-0 on the body to allow flex shrinking below content size', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      const bodyText = screen.getByText('This is a take-home assignment.');
      const bodyContainer = bodyText.parentElement!;

      expect(bodyContainer.className).toContain('min-h-0');
    });
  });

  describe('modal interactions', () => {
    it('should open the modal when the bubble is clicked', () => {
      // Mark as seen so auto-open doesn't fire
      localStorage.setItem('lemondo-evaluator-seen', '1');
      renderWithRouter(<AssignmentBanner />);

      // Advance past bubble delay
      act(() => { vi.advanceTimersByTime(600); });

      const bubble = screen.getByLabelText('A Message to Evaluators');
      act(() => { bubble.click(); });

      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    it('should close the modal when the close button is clicked', () => {
      renderWithRouter(<AssignmentBanner />);
      advancePastBubbleAndModal();

      expect(screen.getByRole('dialog')).toBeInTheDocument();

      const closeButton = screen.getByLabelText('Close');
      act(() => { closeButton.click(); });

      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });
  });
});
