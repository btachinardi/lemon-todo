import { describe, it, expect, vi } from 'vitest';
import { render } from '@testing-library/react';
import { SortableTaskCard } from './SortableTaskCard';
import { createTask } from '@/test/factories';

vi.mock('@dnd-kit/sortable', () => ({
  useSortable: () => ({
    attributes: { role: 'button', tabIndex: 0 },
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
    transition: null,
    isDragging: false,
  }),
}));

vi.mock('@dnd-kit/utilities', () => ({
  CSS: { Transform: { toString: () => null } },
}));

describe('SortableTaskCard', () => {
  it('should apply touch-action none to prevent scroll conflicts during drag', () => {
    const task = createTask({ title: 'Touch test' });
    const { container } = render(<SortableTaskCard task={task} />);

    // The outermost wrapper div (with data-onboarding="task-card") is the drag handle.
    // It must have touch-action: none so the browser doesn't compete with drag gestures.
    const wrapper = container.querySelector('[data-onboarding="task-card"]') as HTMLElement;
    expect(wrapper).not.toBeNull();
    expect(wrapper.style.touchAction).toBe('none');
  });
});
