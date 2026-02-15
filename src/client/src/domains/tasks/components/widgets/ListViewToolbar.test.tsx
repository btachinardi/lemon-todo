import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ListViewToolbar } from './ListViewToolbar';
import { GroupBy } from '../../types/grouping.types';

describe('ListViewToolbar', () => {
  const defaultProps = {
    groupBy: GroupBy.None as GroupBy,
    splitCompleted: false,
    onGroupByChange: vi.fn(),
    onSplitCompletedChange: vi.fn(),
  };

  it('should render the toolbar with correct role', () => {
    render(<ListViewToolbar {...defaultProps} />);
    expect(screen.getByRole('toolbar', { name: /list view options/i })).toBeInTheDocument();
  });

  it('should render the group-by select with current value', () => {
    render(<ListViewToolbar {...defaultProps} groupBy={GroupBy.None} />);
    expect(screen.getByRole('combobox', { name: /group by/i })).toBeInTheDocument();
  });

  it('should display the current grouping value in the trigger', () => {
    render(<ListViewToolbar {...defaultProps} groupBy={GroupBy.Day} />);
    expect(screen.getByText('Day')).toBeInTheDocument();
  });

  it('should render the split-completed toggle', () => {
    render(<ListViewToolbar {...defaultProps} />);
    expect(screen.getByRole('button', { name: /split done/i })).toBeInTheDocument();
  });

  it('should call onSplitCompletedChange when toggle is clicked', async () => {
    const user = userEvent.setup();
    const onSplitCompletedChange = vi.fn();
    render(
      <ListViewToolbar
        {...defaultProps}
        splitCompleted={false}
        onSplitCompletedChange={onSplitCompletedChange}
      />,
    );

    await user.click(screen.getByRole('button', { name: /split done/i }));
    expect(onSplitCompletedChange).toHaveBeenCalledWith(true);
  });

  it('should show pressed state when splitCompleted is true', () => {
    render(<ListViewToolbar {...defaultProps} splitCompleted={true} />);
    const btn = screen.getByRole('button', { name: /split done/i });
    expect(btn).toHaveAttribute('aria-pressed', 'true');
  });
});
