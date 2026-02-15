import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TaskDetailSheet } from './TaskDetailSheet';
import { createTask } from '@/test/factories';
import { Priority, TaskStatus } from '../../types/task.types';

const testTask = createTask({
  id: 'task-1',
  title: 'Buy groceries',
  description: 'Milk, eggs, bread',
  priority: Priority.Medium,
  status: TaskStatus.Todo,
  dueDate: '2026-03-15',
  tags: ['shopping', 'urgent'],
});

function renderSheet(overrides: Partial<React.ComponentProps<typeof TaskDetailSheet>> = {}) {
  const defaultProps: React.ComponentProps<typeof TaskDetailSheet> = {
    taskId: 'task-1',
    onClose: vi.fn(),
    task: testTask,
    isLoading: false,
    isError: false,
    onUpdateTitle: vi.fn(),
    onUpdateDescription: vi.fn(),
    onUpdatePriority: vi.fn(),
    onUpdateDueDate: vi.fn(),
    onAddTag: vi.fn(),
    onRemoveTag: vi.fn(),
    onDelete: vi.fn(),
    isDeleting: false,
    ...overrides,
  };
  return { ...render(<TaskDetailSheet {...defaultProps} />), props: defaultProps };
}

describe('TaskDetailSheet', () => {
  it('should not render content when taskId is null', () => {
    renderSheet({ taskId: null });
    expect(screen.queryByText('Buy groceries')).not.toBeInTheDocument();
  });

  it('should show skeleton while loading', () => {
    renderSheet({ isLoading: true, task: undefined });
    expect(screen.getByTestId('task-detail-skeleton')).toBeInTheDocument();
  });

  it('should render task title when loaded', () => {
    renderSheet();
    expect(screen.getByText('Buy groceries')).toBeInTheDocument();
  });

  it('should render task description', () => {
    renderSheet();
    expect(screen.getByDisplayValue('Milk, eggs, bread')).toBeInTheDocument();
  });

  it('should render tags', () => {
    renderSheet();
    expect(screen.getByText('shopping')).toBeInTheDocument();
    expect(screen.getByText('urgent')).toBeInTheDocument();
  });

  it('should call onUpdateTitle on title blur', async () => {
    const onUpdateTitle = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onUpdateTitle });

    // Click the title to enter edit mode
    await user.click(screen.getByText('Buy groceries'));
    const titleInput = screen.getByLabelText('Task title');
    await user.clear(titleInput);
    await user.type(titleInput, 'Buy organic groceries');
    await user.tab(); // blur

    await waitFor(() =>
      expect(onUpdateTitle).toHaveBeenCalledWith('Buy organic groceries'),
    );
  });

  it('should render priority selector with current value', () => {
    renderSheet();
    expect(screen.getByLabelText('Task priority')).toBeInTheDocument();
  });

  it('should show delete confirmation on delete click', async () => {
    const user = userEvent.setup();
    renderSheet();

    await user.click(screen.getByText('Delete task'));
    expect(screen.getByText('Delete this task?')).toBeInTheDocument();
    expect(screen.getByText('Confirm')).toBeInTheDocument();
  });

  it('should call onDelete on confirm', async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onDelete });

    await user.click(screen.getByText('Delete task'));
    await user.click(screen.getByText('Confirm'));

    expect(onDelete).toHaveBeenCalledOnce();
  });

  it('should call onAddTag when adding a tag', async () => {
    const onAddTag = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onAddTag });

    const tagInput = screen.getByLabelText('New tag');
    await user.type(tagInput, 'groceries');
    await user.click(screen.getByRole('button', { name: /add/i }));

    expect(onAddTag).toHaveBeenCalledWith('groceries');
  });

  it('should call onRemoveTag when removing a tag', async () => {
    const onRemoveTag = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onRemoveTag });

    await user.click(screen.getByLabelText('Remove tag shopping'));

    expect(onRemoveTag).toHaveBeenCalledWith('shopping');
  });

  it('should show error state when isError is true', () => {
    renderSheet({ isError: true, task: undefined });
    expect(screen.getByText('Could not load task details.')).toBeInTheDocument();
  });
});
