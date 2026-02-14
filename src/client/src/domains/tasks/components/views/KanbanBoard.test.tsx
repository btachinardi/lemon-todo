import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { KanbanBoard } from './KanbanBoard';
import { createBoard, createBoardTask } from '@/test/factories';

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

    const tasks = [
      createBoardTask({ title: 'Task in Todo', columnId: todoColumnId, position: 0 }),
      createBoardTask({ title: 'Task in Done', columnId: doneColumnId, position: 0 }),
    ];

    render(<KanbanBoard board={board} tasks={tasks} />);
    expect(screen.getByText('Task in Todo')).toBeInTheDocument();
    expect(screen.getByText('Task in Done')).toBeInTheDocument();
  });

  it('sorts columns by position', () => {
    const board = createBoard({
      columns: [
        { id: 'c3', name: 'Third', position: 2, wipLimit: null },
        { id: 'c1', name: 'First', position: 0, wipLimit: null },
        { id: 'c2', name: 'Second', position: 1, wipLimit: null },
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
