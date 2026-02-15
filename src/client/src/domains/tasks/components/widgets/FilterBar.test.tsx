import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FilterBar } from './FilterBar';

function renderFilterBar(overrides: Partial<React.ComponentProps<typeof FilterBar>> = {}) {
  const defaultProps: React.ComponentProps<typeof FilterBar> = {
    searchTerm: '',
    filterPriority: undefined,
    filterStatus: undefined,
    filterTag: undefined,
    onSearchTermChange: vi.fn(),
    onFilterPriorityChange: vi.fn(),
    onFilterStatusChange: vi.fn(),
    onFilterTagChange: vi.fn(),
    onResetFilters: vi.fn(),
    ...overrides,
  };
  return { ...render(<FilterBar {...defaultProps} />), props: defaultProps };
}

describe('FilterBar', () => {
  it('should render search input', () => {
    renderFilterBar();
    expect(screen.getByLabelText('Search tasks')).toBeInTheDocument();
  });

  it('should render priority filter', () => {
    renderFilterBar();
    expect(screen.getByLabelText('Filter by priority')).toBeInTheDocument();
  });

  it('should render status filter', () => {
    renderFilterBar();
    expect(screen.getByLabelText('Filter by status')).toBeInTheDocument();
  });

  it('should not show clear button when no filters active', () => {
    renderFilterBar();
    expect(screen.queryByText('Clear')).not.toBeInTheDocument();
  });

  it('should show clear search button when user types', async () => {
    const user = userEvent.setup();
    renderFilterBar();
    await user.type(screen.getByLabelText('Search tasks'), 'groceries');
    expect(screen.getByLabelText('Clear search')).toBeInTheDocument();
  });

  it('should show tag chip when filterTag is set', () => {
    renderFilterBar({ filterTag: 'work' });
    expect(screen.getByLabelText('Remove tag filter work')).toBeInTheDocument();
  });

  it('should call onFilterTagChange with undefined on chip removal', async () => {
    const user = userEvent.setup();
    const onFilterTagChange = vi.fn();
    renderFilterBar({ filterTag: 'work', onFilterTagChange });
    await user.click(screen.getByLabelText('Remove tag filter work'));
    expect(onFilterTagChange).toHaveBeenCalledWith(undefined);
  });

  it('should call onResetFilters on clear button click', async () => {
    const user = userEvent.setup();
    const onResetFilters = vi.fn();
    renderFilterBar({
      searchTerm: 'test',
      filterPriority: 'High',
      filterStatus: 'Todo',
      filterTag: 'work',
      onResetFilters,
    });
    await user.click(screen.getByText('Clear'));
    expect(onResetFilters).toHaveBeenCalledOnce();
  });
});
