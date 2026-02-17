import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { DueDateLabel } from './DueDateLabel';

/** Format a local Date as YYYY-MM-DD (the format the backend conceptually represents). */
function toDateOnly(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

describe('DueDateLabel', () => {
  it('renders nothing for null dueDate', () => {
    const { container } = render(<DueDateLabel dueDate={null} />);
    expect(container.innerHTML).toBe('');
  });

  it('renders "Today" for today\'s date', () => {
    render(<DueDateLabel dueDate={toDateOnly(new Date())} />);
    expect(screen.getByText('Today')).toBeInTheDocument();
  });

  it('renders "Tomorrow" for tomorrow\'s date', () => {
    const tomorrow = new Date();
    tomorrow.setDate(tomorrow.getDate() + 1);
    render(<DueDateLabel dueDate={toDateOnly(tomorrow)} />);
    expect(screen.getByText('Tomorrow')).toBeInTheDocument();
  });

  it('renders formatted date for other dates', () => {
    const futureDate = new Date();
    futureDate.setFullYear(futureDate.getFullYear() + 1);
    const { container } = render(<DueDateLabel dueDate={toDateOnly(futureDate)} />);
    // Should render something (locale-dependent format), not "Today" or "Tomorrow"
    const span = container.querySelector('span');
    expect(span).not.toBeNull();
    expect(span!.textContent).not.toContain('Today');
    expect(span!.textContent).not.toContain('Tomorrow');
  });

  it('renders "Overdue" for a past date when task is not done', () => {
    const pastDate = new Date();
    pastDate.setDate(pastDate.getDate() - 3);
    render(<DueDateLabel dueDate={toDateOnly(pastDate)} />);
    expect(screen.getByText(/Overdue/)).toBeInTheDocument();
  });

  it('should not show overdue styling for a past date when isDone is true', () => {
    const pastDate = new Date();
    pastDate.setDate(pastDate.getDate() - 3);
    render(<DueDateLabel dueDate={toDateOnly(pastDate)} isDone />);
    const span = document.querySelector('span');
    expect(span).not.toBeNull();
    expect(span!.textContent).not.toContain('Overdue');
    expect(span!.className).not.toContain('text-destructive');
  });

  it('should show "Today" for a done task due today', () => {
    render(<DueDateLabel dueDate={toDateOnly(new Date())} isDone />);
    expect(screen.getByText('Today')).toBeInTheDocument();
  });

  describe('UTC date handling (backend sends DateTimeOffset)', () => {
    it('should display correct date when given a UTC midnight DateTimeOffset string', () => {
      // Backend returns "2026-07-20T00:00:00+00:00" for a due date of July 20.
      // The bug: UTC midnight shifts to Jul 19 in western timezones. Must show day 20.
      render(<DueDateLabel dueDate="2026-07-20T00:00:00+00:00" />);
      const span = document.querySelector('span');
      expect(span).not.toBeNull();
      // Locale-independent: must contain "20" (the correct day), not "19" (the bug)
      expect(span!.textContent).toMatch(/\b20\b/);
      expect(span!.textContent).not.toMatch(/\b19\b/);
    });

    it('should display correct date when given a date-only string', () => {
      render(<DueDateLabel dueDate="2026-07-20" />);
      const span = document.querySelector('span');
      expect(span).not.toBeNull();
      expect(span!.textContent).toMatch(/\b20\b/);
      expect(span!.textContent).not.toMatch(/\b19\b/);
    });

    it('should display correct date when given a UTC midnight DateTimeOffset with fractional seconds', () => {
      // EF Core SQLite stores: "2026-07-20T00:00:00.0000000+00:00"
      render(<DueDateLabel dueDate="2026-07-20T00:00:00.0000000+00:00" />);
      const span = document.querySelector('span');
      expect(span).not.toBeNull();
      expect(span!.textContent).toMatch(/\b20\b/);
      expect(span!.textContent).not.toMatch(/\b19\b/);
    });
  });
});
