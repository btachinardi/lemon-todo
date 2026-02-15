import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { ListSkeleton } from './ListSkeleton';

describe('ListSkeleton', () => {
  it('should render skeleton container', () => {
    render(<ListSkeleton />);
    expect(screen.getByTestId('list-skeleton')).toBeInTheDocument();
  });
});
