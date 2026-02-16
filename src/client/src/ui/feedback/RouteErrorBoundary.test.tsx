import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { RouteErrorBoundary } from './RouteErrorBoundary';

// Suppress console.error from React error boundary
beforeEach(() => {
  vi.spyOn(console, 'error').mockImplementation(() => {});
});

function ThrowingComponent({ shouldThrow }: { shouldThrow: boolean }) {
  if (shouldThrow) throw new Error('Test crash');
  return <p>Content works</p>;
}

describe('RouteErrorBoundary', () => {
  it('should render children when no error', () => {
    render(
      <RouteErrorBoundary>
        <p>Child content</p>
      </RouteErrorBoundary>,
    );
    expect(screen.getByText('Child content')).toBeInTheDocument();
  });

  it('should show fallback when child throws', () => {
    render(
      <RouteErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </RouteErrorBoundary>,
    );
    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
    expect(screen.getByText('Try again')).toBeInTheDocument();
    expect(screen.getByText('Go home')).toBeInTheDocument();
  });

  it('should recover when try again is clicked', async () => {
    const user = userEvent.setup();
    let shouldThrow = true;

    function ConditionalThrower() {
      if (shouldThrow) throw new Error('Test crash');
      return <p>Recovered</p>;
    }

    render(
      <RouteErrorBoundary>
        <ConditionalThrower />
      </RouteErrorBoundary>,
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();

    // Fix the "error" before clicking recover
    shouldThrow = false;
    await user.click(screen.getByText('Try again'));

    expect(screen.getByText('Recovered')).toBeInTheDocument();
  });

  it('should render test id for identification', () => {
    render(
      <RouteErrorBoundary>
        <ThrowingComponent shouldThrow={true} />
      </RouteErrorBoundary>,
    );
    expect(screen.getByTestId('route-error-boundary')).toBeInTheDocument();
  });
});
