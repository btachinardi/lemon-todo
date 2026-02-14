import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { KanbanBoard } from './KanbanBoard';
import { createBoard, createTask, createTaskCard } from '@/test/factories';
import { TaskStatus } from '../../types/task.types';

describe('KanbanBoard', () => {
  it('renders all columns from board', () => {
    const board = createBoard();
    render(<KanbanBoard board={board} tasks={[]} />);
    expect(screen.getByText('To Do')).toBeInTheDocument();
    expect(screen.getByText('In Progress')).toBeInTheDocument();
    expect(screen.getByText('Done')).toBeInTheDocument();
  });

  it('places tasks in correct columns', () => {
    const board = createBoard();
    const todoColumnId = board.columns[0].id;
    const doneColumnId = board.columns[2].id;

    const task1 = createTask({ title: 'Task in Todo' });
    const task2 = createTask({ title: 'Task in Done' });

    board.cards = [
      createTaskCard({ taskId: task1.id, columnId: todoColumnId, position: 0 }),
      createTaskCard({ taskId: task2.id, columnId: doneColumnId, position: 0 }),
    ];

    render(<KanbanBoard board={board} tasks={[task1, task2]} />);
    expect(screen.getByText('Task in Todo')).toBeInTheDocument();
    expect(screen.getByText('Task in Done')).toBeInTheDocument();
  });

  it('sorts columns by position', () => {
    const board = createBoard({
      columns: [
        { id: 'c3', name: 'Third', targetStatus: TaskStatus.Done, position: 2, maxTasks: null },
        { id: 'c1', name: 'First', targetStatus: TaskStatus.Todo, position: 0, maxTasks: null },
        { id: 'c2', name: 'Second', targetStatus: TaskStatus.InProgress, position: 1, maxTasks: null },
      ],
    });

    const { container } = render(<KanbanBoard board={board} tasks={[]} />);
    const headings = container.querySelectorAll('h3');
    expect(headings[0].textContent).toBe('First');
    expect(headings[1].textContent).toBe('Second');
    expect(headings[2].textContent).toBe('Third');
  });

  it('shows empty state for columns with no tasks', () => {
    const board = createBoard();
    render(<KanbanBoard board={board} tasks={[]} />);
    const emptyMessages = screen.getAllByText('No tasks');
    expect(emptyMessages.length).toBe(3);
  });
});
