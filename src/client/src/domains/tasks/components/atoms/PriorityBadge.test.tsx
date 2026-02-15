import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import * as fc from 'fast-check';
import { PriorityBadge } from './PriorityBadge';
import { Priority } from '../../types/task.types';

describe('PriorityBadge', () => {
  it('renders nothing for Priority.None', () => {
    const { container } = render(<PriorityBadge priority={Priority.None} />);
    expect(container.innerHTML).toBe('');
  });

  it('renders Low priority', () => {
    render(<PriorityBadge priority={Priority.Low} />);
    expect(screen.getByText('Low')).toBeInTheDocument();
  });

  it('renders Medium priority', () => {
    render(<PriorityBadge priority={Priority.Medium} />);
    expect(screen.getByText('Medium')).toBeInTheDocument();
  });

  it('renders High priority', () => {
    render(<PriorityBadge priority={Priority.High} />);
    expect(screen.getByText('High')).toBeInTheDocument();
  });

  it('renders Critical priority', () => {
    render(<PriorityBadge priority={Priority.Critical} />);
    expect(screen.getByText('Critical')).toBeInTheDocument();
  });

  it('property: any non-None priority renders a badge', () => {
    const nonNonePriorities = [Priority.Low, Priority.Medium, Priority.High, Priority.Critical];
    fc.assert(
      fc.property(
        fc.constantFrom(...nonNonePriorities),
        (priority) => {
          const { container } = render(<PriorityBadge priority={priority} />);
          expect(container.querySelector('[data-slot="badge"]')).not.toBeNull();
        },
      ),
    );
  });
});
