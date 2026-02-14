import type { BoardTask } from '@/domains/tasks/types/task.types';
import { Priority, TaskStatus } from '@/domains/tasks/types/task.types';
import type { Board, Column } from '@/domains/tasks/types/board.types';

let counter = 0;
function nextId(): string {
  counter++;
  return `00000000-0000-0000-0000-${String(counter).padStart(12, '0')}`;
}

export function createBoardTask(overrides: Partial<BoardTask> = {}): BoardTask {
  return {
    id: nextId(),
    title: 'Test Task',
    description: null,
    priority: Priority.None,
    status: TaskStatus.Todo,
    dueDate: null,
    tags: [],
    columnId: null,
    position: 0,
    isArchived: false,
    isDeleted: false,
    completedAt: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

export function createColumn(overrides: Partial<Column> = {}): Column {
  return {
    id: nextId(),
    name: 'Test Column',
    position: 0,
    wipLimit: null,
    ...overrides,
  };
}

export function createBoard(overrides: Partial<Board> = {}): Board {
  return {
    id: nextId(),
    name: 'Test Board',
    columns: [
      createColumn({ name: 'To Do', position: 0 }),
      createColumn({ name: 'In Progress', position: 1 }),
      createColumn({ name: 'Done', position: 2 }),
    ],
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}
