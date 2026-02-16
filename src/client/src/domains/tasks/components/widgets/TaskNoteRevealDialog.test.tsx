import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { TaskNoteRevealDialog } from './TaskNoteRevealDialog';

// Mock i18next — return key as value for deterministic assertions
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => key,
  }),
}));

describe('TaskNoteRevealDialog', () => {
  const defaultProps = {
    open: true,
    onOpenChange: vi.fn(),
    onReveal: vi.fn(),
    isPending: false,
    error: null as Error | null,
    revealedNote: null as string | null,
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should render password form when no note is revealed', () => {
    render(<TaskNoteRevealDialog {...defaultProps} />);

    expect(screen.getByText('tasks.noteRevealDialog.title')).toBeInTheDocument();
    expect(screen.getByText('tasks.noteRevealDialog.description')).toBeInTheDocument();
    expect(screen.getByText('tasks.noteRevealDialog.password')).toBeInTheDocument();
  });

  it('should render password input with correct type', () => {
    render(<TaskNoteRevealDialog {...defaultProps} />);

    const passwordInput = screen.getByPlaceholderText('tasks.noteRevealDialog.passwordPlaceholder');
    expect(passwordInput).toHaveAttribute('type', 'password');
  });

  it('should have submit button disabled when password is empty', () => {
    render(<TaskNoteRevealDialog {...defaultProps} />);

    const submitButton = screen.getByText('tasks.noteRevealDialog.submit');
    expect(submitButton).toBeDisabled();
  });

  it('should enable submit button when password is entered', async () => {
    const user = userEvent.setup();
    render(<TaskNoteRevealDialog {...defaultProps} />);

    await user.type(
      screen.getByPlaceholderText('tasks.noteRevealDialog.passwordPlaceholder'),
      'mypassword',
    );

    expect(screen.getByText('tasks.noteRevealDialog.submit')).not.toBeDisabled();
  });

  it('should call onReveal with password when submit is clicked', async () => {
    const user = userEvent.setup();
    render(<TaskNoteRevealDialog {...defaultProps} />);

    await user.type(
      screen.getByPlaceholderText('tasks.noteRevealDialog.passwordPlaceholder'),
      'mypassword',
    );
    await user.click(screen.getByText('tasks.noteRevealDialog.submit'));

    expect(defaultProps.onReveal).toHaveBeenCalledWith('mypassword');
  });

  it('should call onReveal on Enter keypress in password field', async () => {
    const user = userEvent.setup();
    render(<TaskNoteRevealDialog {...defaultProps} />);

    const input = screen.getByPlaceholderText('tasks.noteRevealDialog.passwordPlaceholder');
    await user.type(input, 'mypassword');
    await user.keyboard('{Enter}');

    expect(defaultProps.onReveal).toHaveBeenCalledWith('mypassword');
  });

  it('should show submitting text when isPending', () => {
    render(<TaskNoteRevealDialog {...defaultProps} isPending={true} />);

    expect(screen.getByText('tasks.noteRevealDialog.submitting')).toBeInTheDocument();
  });

  it('should show password error on 401 response', () => {
    const error = new Error('Request failed with status 401');
    render(<TaskNoteRevealDialog {...defaultProps} error={error} />);

    expect(screen.getByText('tasks.noteRevealDialog.passwordError')).toBeInTheDocument();
  });

  it('should not show password error on non-401 error', () => {
    const error = new Error('Network error');
    render(<TaskNoteRevealDialog {...defaultProps} error={error} />);

    expect(screen.queryByText('tasks.noteRevealDialog.passwordError')).not.toBeInTheDocument();
  });

  it('should show revealed note content when revealedNote is provided', () => {
    render(<TaskNoteRevealDialog {...defaultProps} revealedNote="Secret content here" />);

    expect(screen.getByText('Secret content here')).toBeInTheDocument();
    expect(screen.getByText('tasks.noteRevealDialog.revealedDescription')).toBeInTheDocument();
  });

  it('should show hide button when note is revealed', () => {
    render(<TaskNoteRevealDialog {...defaultProps} revealedNote="Secret content" />);

    expect(screen.getByText('tasks.noteRevealDialog.hide')).toBeInTheDocument();
  });

  it('should show countdown timer when note is revealed', () => {
    render(<TaskNoteRevealDialog {...defaultProps} revealedNote="Secret content" />);

    expect(screen.getByText('tasks.noteRevealDialog.autoHide')).toBeInTheDocument();
    expect(screen.getByText('30s')).toBeInTheDocument();
  });

  it('should call onOpenChange(false) when hide button is clicked', async () => {
    const user = userEvent.setup();
    render(<TaskNoteRevealDialog {...defaultProps} revealedNote="Secret content" />);

    await user.click(screen.getByText('tasks.noteRevealDialog.hide'));
    expect(defaultProps.onOpenChange).toHaveBeenCalledWith(false);
  });

  it('should call onOpenChange when cancel button is clicked in password form', async () => {
    const user = userEvent.setup();
    render(<TaskNoteRevealDialog {...defaultProps} />);

    await user.click(screen.getByText('common.cancel'));
    expect(defaultProps.onOpenChange).toHaveBeenCalledWith(false);
  });
});
