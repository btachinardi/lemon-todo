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
    // Ripple elements are decorative divs with the ripple animation
    const ripples = container.querySelectorAll('[data-testid="ripple"]');
    expect(ripples.length).toBeGreaterThanOrEqual(2);
  });

  it('should center the ripple container on the shadow floor', () => {
    const { container } = render(<AppLoadingScreen />);
    const rippleContainer = container.querySelector('[data-testid="ripple"]')!.parentElement!;
    // Ripple container must be vertically aligned with the shadow (bottom-1 = center of h-2 shadow)
    expect(rippleContainer.className).toContain('bottom-1');
    // Must NOT have translate-y that pushes ripple below the floor
    expect(rippleContainer.className).not.toMatch(/\btranslate-y-/);
  });

  it('should horizontally center the ripple container on the icon', () => {
    const { container } = render(<AppLoadingScreen />);
    const rippleContainer = container.querySelector('[data-testid="ripple"]')!.parentElement!;
    expect(rippleContainer.className).toContain('left-1/2');
    expect(rippleContainer.className).toContain('-translate-x-1/2');
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
