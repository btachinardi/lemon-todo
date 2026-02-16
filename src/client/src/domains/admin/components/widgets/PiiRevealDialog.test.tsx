import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { PiiRevealDialog } from './PiiRevealDialog';

// Mock i18next — return key as value for deterministic assertions
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, params?: Record<string, unknown>) => {
      if (params) return `${key}:${JSON.stringify(params)}`;
      return key;
    },
  }),
}));

describe('PiiRevealDialog', () => {
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
    render(<PiiRevealDialog {...defaultProps} />);

    expect(screen.getByText('admin.piiRevealDialog.title')).toBeInTheDocument();
    expect(screen.getByText('admin.piiRevealDialog.reason')).toBeInTheDocument();
    expect(screen.getByText('admin.piiRevealDialog.comments')).toBeInTheDocument();
    expect(screen.getByText('admin.piiRevealDialog.password')).toBeInTheDocument();
    expect(screen.getByText('admin.piiRevealDialog.auditWarning')).toBeInTheDocument();
  });

  it('should have submit button disabled when form is empty', () => {
    render(<PiiRevealDialog {...defaultProps} />);

    const submitButton = screen.getByText('admin.piiRevealDialog.submit');
    expect(submitButton).toBeDisabled();
  });

  it('should not show reasonDetails field initially (no reason selected)', () => {
    render(<PiiRevealDialog {...defaultProps} />);

    // reasonDetails field should not be visible until "Other" is selected
    expect(screen.queryByText('admin.piiRevealDialog.reasonDetails')).not.toBeInTheDocument();
  });

  it('should show password error when mutation returns 401', () => {
    const error = new Error('Request failed with status 401');
    render(<PiiRevealDialog {...defaultProps} error={error} />);

    expect(screen.getByText('admin.piiRevealDialog.passwordError')).toBeInTheDocument();
  });

  it('should not show password error when error is not 401', () => {
    const error = new Error('Network error');
    render(<PiiRevealDialog {...defaultProps} error={error} />);

    expect(screen.queryByText('admin.piiRevealDialog.passwordError')).not.toBeInTheDocument();
  });

  it('should call onOpenChange when cancel is clicked', async () => {
    const user = userEvent.setup();
    render(<PiiRevealDialog {...defaultProps} />);

    await user.click(screen.getByText('common.cancel'));
    expect(defaultProps.onOpenChange).toHaveBeenCalledWith(false);
  });

  it('should show submitting text when isPending', () => {
    render(<PiiRevealDialog {...defaultProps} isPending={true} />);

    expect(screen.getByText('admin.piiRevealDialog.submitting')).toBeInTheDocument();
  });

  it('should render description text', () => {
    render(<PiiRevealDialog {...defaultProps} />);

    expect(screen.getByText('admin.piiRevealDialog.description')).toBeInTheDocument();
  });

  it('should render reason select with combobox role', () => {
    render(<PiiRevealDialog {...defaultProps} />);

    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('should render password input', () => {
    render(<PiiRevealDialog {...defaultProps} />);

    const passwordInput = screen.getByPlaceholderText('admin.piiRevealDialog.passwordPlaceholder');
    expect(passwordInput).toBeInTheDocument();
    expect(passwordInput).toHaveAttribute('type', 'password');
  });
});
