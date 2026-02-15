import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import * as fc from 'fast-check';
import { TagList } from './TagList';

describe('TagList', () => {
  it('renders nothing for empty tags', () => {
    const { container } = render(<TagList tags={[]} />);
    expect(container.innerHTML).toBe('');
  });

  it('renders all tags', () => {
    render(<TagList tags={['bug', 'feature', 'urgent']} />);
    expect(screen.getByText('bug')).toBeInTheDocument();
    expect(screen.getByText('feature')).toBeInTheDocument();
    expect(screen.getByText('urgent')).toBeInTheDocument();
  });

  it('shows remove buttons when onRemove is provided', () => {
    render(<TagList tags={['bug']} onRemove={() => {}} />);
    expect(screen.getByRole('button', { name: 'Remove tag bug' })).toBeInTheDocument();
  });

  it('calls onRemove with tag name when remove button is clicked', async () => {
    const user = userEvent.setup();
    let removedTag = '';
    render(<TagList tags={['bug']} onRemove={(tag) => { removedTag = tag; }} />);

    await user.click(screen.getByRole('button', { name: 'Remove tag bug' }));
    expect(removedTag).toBe('bug');
  });

  it('property: renders correct number of badges for any tag array', () => {
    fc.assert(
      fc.property(
        fc.array(fc.string({ minLength: 1, maxLength: 20 }), { minLength: 1, maxLength: 10 }),
        (tags) => {
          const uniqueTags = [...new Set(tags)];
          const { container } = render(<TagList tags={uniqueTags} />);
          const badges = container.querySelectorAll('[data-slot="badge"]');
          expect(badges.length).toBe(uniqueTags.length);
        },
      ),
    );
  });
});
