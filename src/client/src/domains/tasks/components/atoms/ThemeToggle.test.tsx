import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeToggle } from './ThemeToggle';

describe('ThemeToggle', () => {
  it('should render a button', () => {
    render(<ThemeToggle theme="dark" onToggle={() => {}} />);
    expect(screen.getByRole('button', { name: /theme/i })).toBeInTheDocument();
  });

  it('should call onToggle when clicked', async () => {
    const onToggle = vi.fn();
    const user = userEvent.setup();
    render(<ThemeToggle theme="dark" onToggle={onToggle} />);

    await user.click(screen.getByRole('button', { name: /theme/i }));
    expect(onToggle).toHaveBeenCalledOnce();
  });

  it('should show dark theme label when theme is dark', () => {
    render(<ThemeToggle theme="dark" onToggle={() => {}} />);
    expect(screen.getByRole('button', { name: 'Dark theme' })).toBeInTheDocument();
  });

  it('should show light theme label when theme is light', () => {
    render(<ThemeToggle theme="light" onToggle={() => {}} />);
    expect(screen.getByRole('button', { name: 'Light theme' })).toBeInTheDocument();
  });

  it('should show system theme label when theme is system', () => {
    render(<ThemeToggle theme="system" onToggle={() => {}} />);
    expect(screen.getByRole('button', { name: 'System theme' })).toBeInTheDocument();
  });

  it('should show visible label text when showLabel is true', () => {
    render(<ThemeToggle theme="dark" onToggle={() => {}} showLabel />);
    expect(screen.getByText('Dark theme')).toBeVisible();
  });

  it('should not show visible label text by default', () => {
    render(<ThemeToggle theme="dark" onToggle={() => {}} />);
    // "Dark theme" should only exist as aria-label, not as visible text
    expect(screen.queryByText('Dark theme')).not.toBeInTheDocument();
  });
});
