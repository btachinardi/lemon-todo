import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TaskListView } from './TaskListView';
import { createTask } from '@/test/factories';
import { Priority, TaskStatus } from '../../types/task.types';

describe('TaskListView', () => {
  it('shows empty state when no tasks', () => {
    render(<TaskListView tasks={[]} />);
    expect(screen.getByText('No tasks yet')).toBeInTheDocument();
    expect(screen.getByText('Add a task above to get started.')).toBeInTheDocument();
  });

  it('renders all tasks', () => {
    const tasks = [
      createTask({ title: 'Task Alpha' }),
      createTask({ title: 'Task Beta' }),
      createTask({ title: 'Task Gamma' }),
    ];
    render(<TaskListView tasks={tasks} />);
    expect(screen.getByText('Task Alpha')).toBeInTheDocument();
    expect(screen.getByText('Task Beta')).toBeInTheDocument();
    expect(screen.getByText('Task Gamma')).toBeInTheDocument();
  });

  it('renders task metadata (status, priority, tags)', () => {
    const tasks = [
      createTask({
        title: 'Rich Task',
        status: TaskStatus.InProgress,
        priority: Priority.High,
        tags: ['backend'],
      }),
    ];
    render(<TaskListView tasks={tasks} />);
    expect(screen.getByText('In Progress')).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
    expect(screen.getByText('backend')).toBeInTheDocument();
  });

  it('calls onCompleteTask when check button is clicked', async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    const tasks = [createTask({ id: 'task-99' })];
    render(<TaskListView tasks={tasks} onCompleteTask={onComplete} />);

    await user.click(screen.getByRole('button', { name: /mark as complete/i }));
    expect(onComplete).toHaveBeenCalledWith('task-99');
  });

  it('applies line-through for completed tasks', () => {
    const tasks = [createTask({ title: 'Done Task', status: TaskStatus.Done })];
    render(<TaskListView tasks={tasks} />);
    const title = screen.getByText('Done Task');
    expect(title.className).toContain('line-through');
  });
});
