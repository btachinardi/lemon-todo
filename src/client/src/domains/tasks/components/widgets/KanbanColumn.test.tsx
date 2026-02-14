import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { KanbanColumn } from './KanbanColumn';
import { createColumn, createBoardTask } from '@/test/factories';

describe('KanbanColumn', () => {
  it('renders column name', () => {
    const column = createColumn({ name: 'In Progress' });
    render(<KanbanColumn column={column} tasks={[]} />);
    expect(screen.getByText('In Progress')).toBeInTheDocument();
  });

  it('renders task count', () => {
    const column = createColumn();
    const tasks = [createBoardTask(), createBoardTask()];
    render(<KanbanColumn column={column} tasks={tasks} />);
    expect(screen.getByText('2')).toBeInTheDocument();
  });

  it('shows max tasks limit when set', () => {
    const column = createColumn({ maxTasks: 5 });
    const tasks = [createBoardTask(), createBoardTask()];
    render(<KanbanColumn column={column} tasks={tasks} />);
    expect(screen.getByText('2/5')).toBeInTheDocument();
  });

  it('renders task cards', () => {
    const column = createColumn();
    const tasks = [
      createBoardTask({ title: 'Task A' }),
      createBoardTask({ title: 'Task B' }),
    ];
    render(<KanbanColumn column={column} tasks={tasks} />);
    expect(screen.getByText('Task A')).toBeInTheDocument();
    expect(screen.getByText('Task B')).toBeInTheDocument();
  });

  it('shows empty state when no tasks', () => {
    const column = createColumn();
    render(<KanbanColumn column={column} tasks={[]} />);
    expect(screen.getByText('No tasks')).toBeInTheDocument();
  });
});
