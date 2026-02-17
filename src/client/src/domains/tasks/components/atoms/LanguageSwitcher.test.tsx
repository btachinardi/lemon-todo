import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { LanguageSwitcher } from './LanguageSwitcher';

describe('LanguageSwitcher', () => {
  it('should render a button with screen-reader text by default', () => {
    render(<LanguageSwitcher />);
    expect(screen.getByRole('button', { name: /language/i })).toBeInTheDocument();
  });

  it('should show visible label text when showLabel is true', () => {
    render(<LanguageSwitcher showLabel />);
    const labels = screen.getAllByText('Language');
    const visibleLabel = labels.find((el) => !el.classList.contains('sr-only'));
    expect(visibleLabel).toBeDefined();
  });

  it('should only have sr-only label text by default', () => {
    render(<LanguageSwitcher />);
    const label = screen.getByText('Language');
    expect(label).toHaveClass('sr-only');
  });
});
