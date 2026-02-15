import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { BoardSkeleton } from './BoardSkeleton';

describe('BoardSkeleton', () => {
  it('should render skeleton container', () => {
    render(<BoardSkeleton />);
    expect(screen.getByTestId('board-skeleton')).toBeInTheDocument();
  });
});
