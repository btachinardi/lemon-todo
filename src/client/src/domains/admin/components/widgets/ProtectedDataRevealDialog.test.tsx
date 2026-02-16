import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { ProtectedDataRevealDialog } from './ProtectedDataRevealDialog';
import { ApiRequestError } from '@/lib/api-client';

// Mock i18next — return key as value for deterministic assertions
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, params?: Record<string, unknown>) => {
      if (params) return `${key}:${JSON.stringify(params)}`;
      return key;
    },
  }),
}));

describe('ProtectedDataRevealDialog', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    onReveal: vi.fn(),
    isPending: false,
    error: null,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render all form fields when open', () => {
    render(<ProtectedDataRevealDialog {...defaultProps} />);

    expect(screen.getByText('admin.protectedDataRevealDialog.title')).toBeInTheDocument();
    expect(screen.getByText('admin.protectedDataRevealDialog.reason')).toBeInTheDocument();
    expect(screen.getByText('admin.protectedDataRevealDialog.comments')).toBeInTheDocument();
    expect(screen.getByText('admin.protectedDataRevealDialog.password')).toBeInTheDocument();
    expect(screen.getByText('admin.protectedDataRevealDialog.auditWarning')).toBeInTheDocument();
  });

  it('should have submit button disabled when form is empty', () => {
    render(<ProtectedDataRevealDialog {...defaultProps} />);

    const submitButton = screen.getByText('admin.protectedDataRevealDialog.submit');
    expect(submitButton).toBeDisabled();
  });

  it('should not show reasonDetails field initially (no reason selected)', () => {
    render(<ProtectedDataRevealDialog {...defaultProps} />);

    // reasonDetails field should not be visible until "Other" is selected
    expect(screen.queryByText('admin.protectedDataRevealDialog.reasonDetails')).not.toBeInTheDocument();
  });

  it('should show password error when ApiRequestError has status 401', () => {
    const error = new ApiRequestError(401, {
      type: 'unauthorized',
      title: 'Invalid password. Re-authentication failed.',
      status: 401,
    });
    render(<ProtectedDataRevealDialog {...defaultProps} error={error} />);

    expect(screen.getByText('admin.protectedDataRevealDialog.passwordError')).toBeInTheDocument();
  });

  it('should not show password error when error is not 401', () => {
    const error = new ApiRequestError(500, {
      type: 'unknown_error',
      title: 'Internal server error',
      status: 500,
    });
    render(<ProtectedDataRevealDialog {...defaultProps} error={error} />);

    expect(screen.queryByText('admin.protectedDataRevealDialog.passwordError')).not.toBeInTheDocument();
  });

  it('should show generic error for non-401 errors', () => {
    const error = new ApiRequestError(500, {
      type: 'unknown_error',
      title: 'Internal server error',
      status: 500,
    });
    render(<ProtectedDataRevealDialog {...defaultProps} error={error} />);

    expect(screen.getByText('admin.protectedDataRevealDialog.genericError')).toBeInTheDocument();
  });

  it('should not show generic error when error is password error', () => {
    const error = new ApiRequestError(401, {
      type: 'unauthorized',
      title: 'Invalid password. Re-authentication failed.',
      status: 401,
    });
    render(<ProtectedDataRevealDialog {...defaultProps} error={error} />);

    expect(screen.queryByText('admin.protectedDataRevealDialog.genericError')).not.toBeInTheDocument();
  });

  it('should call onOpenChange when cancel is clicked', async () => {
    const user = userEvent.setup();
    render(<ProtectedDataRevealDialog {...defaultProps} />);

    await user.click(screen.getByText('common.cancel'));
    expect(defaultProps.onOpenChange).toHaveBeenCalledWith(false);
  });

  it('should show submitting text when isPending', () => {
    render(<ProtectedDataRevealDialog {...defaultProps} isPending={true} />);

    expect(screen.getByText('admin.protectedDataRevealDialog.submitting')).toBeInTheDocument();
  });

  it('should render description text', () => {
    render(<ProtectedDataRevealDialog {...defaultProps} />);

    expect(screen.getByText('admin.protectedDataRevealDialog.description')).toBeInTheDocument();
  });

  it('should render reason select with combobox role', () => {
    render(<ProtectedDataRevealDialog {...defaultProps} />);

    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('should render password input', () => {
    render(<ProtectedDataRevealDialog {...defaultProps} />);

    const passwordInput = screen.getByPlaceholderText('admin.protectedDataRevealDialog.passwordPlaceholder');
    expect(passwordInput).toBeInTheDocument();
    expect(passwordInput).toHaveAttribute('type', 'password');
  });

  describe('dev auto-fill', () => {
    it('should show auto-fill button when devPassword is provided', () => {
      render(<ProtectedDataRevealDialog {...defaultProps} devPassword="SysAdmin1234" />);
      expect(screen.getByText('common.devAutoFill')).toBeInTheDocument();
    });
    it('should not show auto-fill button when devPassword is null', () => {
      render(<ProtectedDataRevealDialog {...defaultProps} devPassword={null} />);
      expect(screen.queryByText('common.devAutoFill')).not.toBeInTheDocument();
    });
    it('should not show auto-fill button when devPassword is not provided', () => {
      render(<ProtectedDataRevealDialog {...defaultProps} />);
      expect(screen.queryByText('common.devAutoFill')).not.toBeInTheDocument();
    });
    it('should fill password field when auto-fill button is clicked', async () => {
      const user = userEvent.setup();
      render(<ProtectedDataRevealDialog {...defaultProps} devPassword="SysAdmin1234" />);
      await user.click(screen.getByText('common.devAutoFill'));
      const passwordInput = screen.getByPlaceholderText('admin.protectedDataRevealDialog.passwordPlaceholder');
      expect(passwordInput).toHaveValue('SysAdmin1234');
    });
  });
});
