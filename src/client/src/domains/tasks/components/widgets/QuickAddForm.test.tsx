import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QuickAddForm } from './QuickAddForm';

describe('QuickAddForm', () => {
  it('renders input and button', () => {
    render(<QuickAddForm onSubmit={() => {}} />);
    expect(screen.getByLabelText('New task title')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /add/i })).toBeInTheDocument();
  });

  it('disables button when input is empty', () => {
    render(<QuickAddForm onSubmit={() => {}} />);
    expect(screen.getByRole('button', { name: /add/i })).toBeDisabled();
  });

  it('enables button when input has text', async () => {
    const user = userEvent.setup();
    render(<QuickAddForm onSubmit={() => {}} />);

    await user.type(screen.getByLabelText('New task title'), 'New task');
    expect(screen.getByRole('button', { name: /add/i })).toBeEnabled();
  });

  it('calls onSubmit with title and clears input', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<QuickAddForm onSubmit={onSubmit} />);

    const input = screen.getByLabelText('New task title');
    await user.type(input, 'Buy milk');
    await user.click(screen.getByRole('button', { name: /add/i }));

    expect(onSubmit).toHaveBeenCalledWith({ title: 'Buy milk' });
    expect(input).toHaveValue('');
  });

  it('does not submit whitespace-only input', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<QuickAddForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText('New task title'), '   ');
    await user.click(screen.getByRole('button', { name: /add/i }));

    expect(onSubmit).not.toHaveBeenCalled();
  });

  it('submits on Enter key', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn();
    render(<QuickAddForm onSubmit={onSubmit} />);

    await user.type(screen.getByLabelText('New task title'), 'Task via Enter{Enter}');
    expect(onSubmit).toHaveBeenCalledWith({ title: 'Task via Enter' });
  });

  it('disables input and button when isLoading', () => {
    render(<QuickAddForm onSubmit={() => {}} isLoading />);
    expect(screen.getByLabelText('New task title')).toBeDisabled();
  });
});
