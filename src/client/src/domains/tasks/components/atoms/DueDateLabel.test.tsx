import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { DueDateLabel } from './DueDateLabel';

describe('DueDateLabel', () => {
  it('renders nothing for null dueDate', () => {
    const { container } = render(<DueDateLabel dueDate={null} />);
    expect(container.innerHTML).toBe('');
  });

  it('renders "Today" for today\'s date', () => {
    const today = new Date();
    today.setHours(23, 59, 59);
    render(<DueDateLabel dueDate={today.toISOString()} />);
    expect(screen.getByText('Today')).toBeInTheDocument();
  });

  it('renders "Tomorrow" for tomorrow\'s date', () => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    tomorrow.setHours(12, 0, 0);
    render(<DueDateLabel dueDate={tomorrow.toISOString()} />);
    expect(screen.getByText('Tomorrow')).toBeInTheDocument();
  });

  it('renders formatted date for other dates', () => {
    const futureDate = new Date();
    futureDate.setFullYear(futureDate.getFullYear() + 1);
    const { container } = render(<DueDateLabel dueDate={futureDate.toISOString()} />);
    // Should render something (locale-dependent format), not "Today" or "Tomorrow"
    const span = container.querySelector('span');
    expect(span).not.toBeNull();
    expect(span!.textContent).not.toContain('Today');
    expect(span!.textContent).not.toContain('Tomorrow');
  });
});
