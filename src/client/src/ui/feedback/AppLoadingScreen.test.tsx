import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { AppLoadingScreen } from './AppLoadingScreen';

describe('AppLoadingScreen', () => {
  it('should render with a status role for accessibility', () => {
    render(<AppLoadingScreen />);
    expect(screen.getByRole('status')).toBeInTheDocument();
  });

  it('should display the LemonDo logo', () => {
    render(<AppLoadingScreen />);
    const logo = screen.getByAltText(/lemon/i);
    expect(logo).toBeInTheDocument();
    expect(logo.tagName).toBe('IMG');
  });

  it('should display loading text', () => {
    render(<AppLoadingScreen />);
    expect(screen.getByText(/loading/i)).toBeInTheDocument();
  });

  it('should render ripple animation elements', () => {
    const { container } = render(<AppLoadingScreen />);
    const ripples = container.querySelectorAll('[data-testid="ripple"]');
    expect(ripples.length).toBeGreaterThanOrEqual(2);
  });

  it('should center ripples via inset-0 m-auto (no translate classes)', () => {
    const { container } = render(<AppLoadingScreen />);
    const ripples = container.querySelectorAll('[data-testid="ripple"]');
    for (const ripple of ripples) {
      // Must use inset-0 + m-auto for centering (avoids translate/transform conflict)
      expect(ripple.className).toContain('inset-0');
      expect(ripple.className).toContain('m-auto');
      // Must NOT use translate classes (they conflict with animation transform)
      expect(ripple.className).not.toMatch(/-translate-[xy]/);
      expect(ripple.className).not.toMatch(/\bleft-1\/2\b/);
      expect(ripple.className).not.toMatch(/\btop-1\/2\b/);
    }
  });

  it('should render ripples behind the logo via z-index layering', () => {
    const { container } = render(<AppLoadingScreen />);
    const ripple = container.querySelector('[data-testid="ripple"]')!;
    const logo = screen.getByAltText(/lemon/i);

    // The logo's ancestor must have a higher z-index than the ripple
    const logoBouncingWrapper = logo.closest('.animate-app-loading-bounce')!;
    expect(logoBouncingWrapper.className).toMatch(/z-1\d/); // z-10 or higher

    // Ripples must NOT have a z-index that would put them above the logo
    expect(ripple.className).not.toMatch(/z-\d/);
  });

  it('should render ripples as ellipses (width > height) for perspective effect', () => {
    const { container } = render(<AppLoadingScreen />);
    const ripple = container.querySelector('[data-testid="ripple"]')!;
    // Must have different width and height classes (elliptical, not circular)
    // w-8 = 2rem width, h-4 = 1rem height (half the width)
    expect(ripple.className).toMatch(/\bw-\d/);
    expect(ripple.className).toMatch(/\bh-\d/);
    expect(ripple.className).not.toMatch(/\bsize-/); // not using equal size
  });

  it('should have aria-live polite for screen readers', () => {
    render(<AppLoadingScreen />);
    const status = screen.getByRole('status');
    expect(status).toHaveAttribute('aria-live', 'polite');
  });

  it('should accept and display a custom message', () => {
    render(<AppLoadingScreen message="Connecting to server..." />);
    expect(screen.getByText('Connecting to server...')).toBeInTheDocument();
  });
});
