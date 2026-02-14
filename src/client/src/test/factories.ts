import type { Task } from '@/domains/tasks/types/task.types';
import { Priority, TaskStatus } from '@/domains/tasks/types/task.types';
import type { Board, Column, TaskCard } from '@/domains/tasks/types/board.types';

let counter = 0;
function nextId(): string {
  counter++;
  return `00000000-0000-0000-0000-${String(counter).padStart(12, '0')}`;
}

export function createTask(overrides: Partial<Task> = {}): Task {
  return {
    id: nextId(),
    title: 'Test Task',
    description: null,
    priority: Priority.None,
    status: TaskStatus.Todo,
    dueDate: null,
    tags: [],
    isArchived: false,
    isDeleted: false,
    completedAt: null,
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

export function createTaskCard(overrides: Partial<TaskCard> = {}): TaskCard {
  return {
    taskId: nextId(),
    columnId: nextId(),
    position: 0,
    ...overrides,
  };
}

export function createColumn(overrides: Partial<Column> = {}): Column {
  return {
    id: nextId(),
    name: 'Test Column',
    targetStatus: TaskStatus.Todo,
    position: 0,
    maxTasks: null,
    ...overrides,
  };
}

export function createBoard(overrides: Partial<Board> = {}): Board {
  return {
    id: nextId(),
    name: 'Test Board',
    columns: [
      createColumn({ name: 'To Do', targetStatus: TaskStatus.Todo, position: 0 }),
      createColumn({ name: 'In Progress', targetStatus: TaskStatus.InProgress, position: 1 }),
      createColumn({ name: 'Done', targetStatus: TaskStatus.Done, position: 2 }),
    ],
    cards: [],
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}
