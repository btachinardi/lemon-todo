import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { EmptyBoard } from './EmptyBoard';

describe('EmptyBoard', () => {
  it('should render empty board message', () => {
    render(<EmptyBoard />);
    expect(screen.getByText('Your board is empty')).toBeInTheDocument();
  });

  it('should show call-to-action text', () => {
    render(<EmptyBoard />);
    expect(screen.getByText('Add a task above to get started.')).toBeInTheDocument();
  });
});
