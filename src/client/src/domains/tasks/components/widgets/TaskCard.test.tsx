import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TaskCard } from './TaskCard';
import { createBoardTask } from '@/test/factories';
import { Priority, TaskStatus } from '../../types/task.types';

describe('TaskCard', () => {
  it('renders task title', () => {
    const task = createBoardTask({ title: 'Buy groceries' });
    render(<TaskCard task={task} />);
    expect(screen.getByText('Buy groceries')).toBeInTheDocument();
  });

  it('renders priority badge when priority is not None', () => {
    const task = createBoardTask({ priority: Priority.High });
    render(<TaskCard task={task} />);
    expect(screen.getByText('High')).toBeInTheDocument();
  });

  it('renders tags', () => {
    const task = createBoardTask({ tags: ['bug', 'urgent'] });
    render(<TaskCard task={task} />);
    expect(screen.getByText('bug')).toBeInTheDocument();
    expect(screen.getByText('urgent')).toBeInTheDocument();
  });

  it('renders due date', () => {
    const today = new Date();
    today.setHours(23, 59, 59);
    const task = createBoardTask({ dueDate: today.toISOString() });
    render(<TaskCard task={task} />);
    expect(screen.getByText('Today')).toBeInTheDocument();
  });

  it('applies line-through style for completed tasks', () => {
    const task = createBoardTask({ status: TaskStatus.Done });
    render(<TaskCard task={task} />);
    const title = screen.getByText('Test Task');
    expect(title.className).toContain('line-through');
  });

  it('calls onComplete when check button is clicked', async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    const task = createBoardTask({ id: 'task-1' });
    render(<TaskCard task={task} onComplete={onComplete} />);

    await user.click(screen.getByRole('button', { name: /mark as complete/i }));
    expect(onComplete).toHaveBeenCalledWith('task-1');
  });

  it('calls onSelect when card is clicked', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    const task = createBoardTask({ id: 'task-2' });
    render(<TaskCard task={task} onSelect={onSelect} />);

    await user.click(screen.getByText('Test Task'));
    expect(onSelect).toHaveBeenCalledWith('task-2');
  });

  it('does not call onSelect when complete button is clicked', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    const onComplete = vi.fn();
    const task = createBoardTask();
    render(<TaskCard task={task} onSelect={onSelect} onComplete={onComplete} />);

    await user.click(screen.getByRole('button', { name: /mark as complete/i }));
    expect(onComplete).toHaveBeenCalled();
    expect(onSelect).not.toHaveBeenCalled();
  });
});
