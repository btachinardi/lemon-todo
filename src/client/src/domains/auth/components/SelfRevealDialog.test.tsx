import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ApiRequestError } from '@/lib/api-client';
import { SelfRevealDialog } from './SelfRevealDialog';

describe('SelfRevealDialog', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    onReveal: vi.fn(),
    isPending: false,
    error: null as Error | null | undefined,
    revealedEmail: null as string | null | undefined,
    revealedDisplayName: null as string | null | undefined,
  };

  function renderDialog(overrides: Partial<typeof defaultProps> = {}) {
    return render(<SelfRevealDialog {...defaultProps} {...overrides} />);
  }

  it('should render password input and submit button', () => {
    renderDialog();

    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /reveal/i })).toBeInTheDocument();
  });

  it('should disable submit when password is empty', () => {
    renderDialog();

    expect(screen.getByRole('button', { name: /reveal/i })).toBeDisabled();
  });

  it('should call onReveal with password on submit', async () => {
    const user = userEvent.setup();
    const onReveal = vi.fn();
    renderDialog({ onReveal });

    await user.type(screen.getByLabelText(/password/i), 'MyPassword123!');
    await user.click(screen.getByRole('button', { name: /reveal/i }));

    expect(onReveal).toHaveBeenCalledWith('MyPassword123!');
  });

  it('should show password error on 401', () => {
    const error = new ApiRequestError(401, {
      type: 'unauthorized',
      title: 'Invalid password.',
      status: 401,
    });
    renderDialog({ error });

    expect(screen.getByText(/incorrect password/i)).toBeInTheDocument();
  });

  it('should show revealed data with countdown after success', () => {
    renderDialog({
      revealedEmail: 'test@lemondo.dev',
      revealedDisplayName: 'Test User',
    });

    expect(screen.getByText('test@lemondo.dev')).toBeInTheDocument();
    expect(screen.getByText('Test User')).toBeInTheDocument();
    // Should show countdown element (the "Auto-hiding in" label)
    expect(screen.getByText(/auto-hiding in/i)).toBeInTheDocument();
  });

  it('should show hide button when data is revealed', () => {
    renderDialog({
      revealedEmail: 'test@lemondo.dev',
      revealedDisplayName: 'Test User',
    });

    expect(screen.getByRole('button', { name: /hide now/i })).toBeInTheDocument();
  });
});
