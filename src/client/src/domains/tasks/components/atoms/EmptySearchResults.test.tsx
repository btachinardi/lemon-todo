import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { EmptySearchResults } from './EmptySearchResults';

describe('EmptySearchResults', () => {
  it('should render no matching tasks message', () => {
    render(<EmptySearchResults onClearFilters={() => {}} />);
    expect(screen.getByText('No matching tasks')).toBeInTheDocument();
  });

  it('should render clear filters button', () => {
    render(<EmptySearchResults onClearFilters={() => {}} />);
    expect(screen.getByText('Clear filters')).toBeInTheDocument();
  });

  it('should call onClearFilters on button click', async () => {
    const onClearFilters = vi.fn();
    const user = userEvent.setup();
    render(<EmptySearchResults onClearFilters={onClearFilters} />);

    await user.click(screen.getByText('Clear filters'));
    expect(onClearFilters).toHaveBeenCalledOnce();
  });
});
