import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TaskListView } from './TaskListView';
import { createTask } from '@/test/factories';
import { Priority, TaskStatus } from '../../types/task.types';
import type { TaskGroup } from '../../types/grouping.types';

/** Helper to wrap tasks in a single flat group (mimics GroupBy.None). */
function flatGroup(tasks: ReturnType<typeof createTask>[], completedTasks: ReturnType<typeof createTask>[] = []): TaskGroup[] {
  if (tasks.length === 0 && completedTasks.length === 0) return [];
  return [{ key: 'all', label: 'All Tasks', tasks, completedTasks }];
}

describe('TaskListView', () => {
  it('shows empty state when no groups', () => {
    render(<TaskListView groups={[]} />);
    expect(screen.getByText('No tasks yet')).toBeInTheDocument();
    expect(screen.getByText('Add a task above to get started.')).toBeInTheDocument();
  });

  it('renders all tasks', () => {
    const tasks = [
      createTask({ title: 'Task Alpha' }),
      createTask({ title: 'Task Beta' }),
      createTask({ title: 'Task Gamma' }),
    ];
    render(<TaskListView groups={flatGroup(tasks)} />);
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
    render(<TaskListView groups={flatGroup(tasks)} />);
    expect(screen.getByText('In Progress')).toBeInTheDocument();
    expect(screen.getByText('High')).toBeInTheDocument();
    expect(screen.getByText('backend')).toBeInTheDocument();
  });

  it('calls onCompleteTask when check button is clicked', async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    const tasks = [createTask({ id: 'task-99' })];
    render(<TaskListView groups={flatGroup(tasks)} onCompleteTask={onComplete} />);

    await user.click(screen.getByRole('button', { name: /mark as complete/i }));
    expect(onComplete).toHaveBeenCalledWith('task-99');
  });

  it('applies line-through for completed tasks', () => {
    const tasks = [createTask({ title: 'Done Task', status: TaskStatus.Done })];
    render(<TaskListView groups={flatGroup(tasks)} />);
    const title = screen.getByText('Done Task');
    expect(title.className).toContain('line-through');
  });

  describe('group headers', () => {
    it('does not show group headers when showGroupHeaders is false', () => {
      const groups: TaskGroup[] = [
        { key: '2026-01-15', label: 'Thu, Jan 15, 2026', tasks: [createTask()], completedTasks: [] },
      ];
      render(<TaskListView groups={groups} showGroupHeaders={false} />);
      expect(screen.queryByText('Thu, Jan 15, 2026')).not.toBeInTheDocument();
    });

    it('shows group headers with count badge when showGroupHeaders is true', () => {
      const groups: TaskGroup[] = [
        { key: '2026-01-15', label: 'Thu, Jan 15, 2026', tasks: [createTask(), createTask()], completedTasks: [createTask({ status: TaskStatus.Done })] },
      ];
      render(<TaskListView groups={groups} showGroupHeaders />);
      expect(screen.getByText('Thu, Jan 15, 2026')).toBeInTheDocument();
      expect(screen.getByText('3')).toBeInTheDocument();
    });
  });

  describe('completed tasks split', () => {
    it('shows "Completed" separator when completedTasks is non-empty', () => {
      const groups: TaskGroup[] = [
        {
          key: 'all',
          label: 'All Tasks',
          tasks: [createTask({ title: 'Active' })],
          completedTasks: [createTask({ title: 'Finished', status: TaskStatus.Done })],
        },
      ];
      render(<TaskListView groups={groups} />);
      expect(screen.getByText('Active')).toBeInTheDocument();
      expect(screen.getByText('Finished')).toBeInTheDocument();
      expect(screen.getByText(/Completed \(1\)/)).toBeInTheDocument();
    });

    it('does not show "Completed" separator when completedTasks is empty', () => {
      const groups: TaskGroup[] = [
        { key: 'all', label: 'All Tasks', tasks: [createTask()], completedTasks: [] },
      ];
      render(<TaskListView groups={groups} />);
      expect(screen.queryByText(/Completed/)).not.toBeInTheDocument();
    });
  });
});
