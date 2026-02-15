import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import { KanbanBoard } from './KanbanBoard';
import { createBoard, createTask, createTaskCard } from '@/test/factories';
import { TaskStatus } from '../../types/task.types';

/**
 * Captured DndContext event handlers. Updated on every render of KanbanBoard
 * because the mock DndContext re-captures props each time.
 */
let dndHandlers: {
  onDragStart?: (event: any) => void;
  onDragOver?: (event: any) => void;
  onDragEnd?: (event: any) => void;
  onDragCancel?: () => void;
};

// Mock @dnd-kit/core — capture DndContext handlers so we can invoke them directly
vi.mock('@dnd-kit/core', () => ({
  DndContext: ({ children, onDragStart, onDragOver, onDragEnd, onDragCancel }: any) => {
    dndHandlers = { onDragStart, onDragOver, onDragEnd, onDragCancel };
    return <>{children}</>;
  },
  DragOverlay: ({ children }: any) => <>{children}</>,
  closestCorners: vi.fn(),
  PointerSensor: class {},
  useSensor: () => ({}),
  useSensors: () => [],
  useDroppable: () => ({ setNodeRef: vi.fn(), isOver: false }),
}));

// Mock @dnd-kit/sortable — provide inert implementations
vi.mock('@dnd-kit/sortable', () => ({
  SortableContext: ({ children }: any) => <>{children}</>,
  verticalListSortingStrategy: {},
  useSortable: () => ({
    attributes: { role: 'button', tabIndex: 0 },
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
    transition: null,
    isDragging: false,
  }),
  arrayMove: (arr: any[], from: number, to: number) => {
    const result = [...arr];
    const [item] = result.splice(from, 1);
    result.splice(to, 0, item);
    return result;
  },
}));

// Mock @dnd-kit/utilities
vi.mock('@dnd-kit/utilities', () => ({
  CSS: { Transform: { toString: () => null } },
}));

/** Creates a minimal DnD event object with `active` and `over` fields. */
function makeDndEvent(activeId: string, overId?: string | null) {
  const mockRect = { top: 0, left: 0, right: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0 };
  return {
    active: { id: activeId, data: { current: {} }, rect: { current: { initial: mockRect, translated: mockRect } } },
    over: overId != null
      ? { id: overId, data: { current: {} }, rect: mockRect, disabled: false }
      : null,
    delta: { x: 0, y: 0 },
    activatorEvent: new Event('pointerdown'),
  };
}

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

describe('KanbanBoard drag-and-drop', () => {
  beforeEach(() => {
    dndHandlers = {};
  });

  it('should call onMoveTask when a card is dragged to a different column', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];
    const inProgressCol = board.columns[1];

    const task = createTask({ title: 'Drag me' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: todoCol.id, position: 0 })];

    render(<KanbanBoard board={board} tasks={[task]} onMoveTask={onMoveTask} />);

    // 1. Start dragging the task
    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(task.id));
    });

    // 2. Drag over the InProgress column (this updates columnItems internally)
    act(() => {
      dndHandlers.onDragOver?.(makeDndEvent(task.id, inProgressCol.id));
    });

    // 3. Drop on the InProgress column
    act(() => {
      dndHandlers.onDragEnd?.(makeDndEvent(task.id, inProgressCol.id));
    });

    // onMoveTask MUST be called with the target column
    expect(onMoveTask).toHaveBeenCalledTimes(1);
    expect(onMoveTask).toHaveBeenCalledWith(task.id, inProgressCol.id, expect.any(Number));
  });

  it('should call onMoveTask with correct column when dropping onto a task in another column', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];
    const inProgressCol = board.columns[1];

    const taskA = createTask({ title: 'Task A' });
    const taskB = createTask({ title: 'Task B' });
    board.cards = [
      createTaskCard({ taskId: taskA.id, columnId: todoCol.id, position: 0 }),
      createTaskCard({ taskId: taskB.id, columnId: inProgressCol.id, position: 0 }),
    ];

    render(<KanbanBoard board={board} tasks={[taskA, taskB]} onMoveTask={onMoveTask} />);

    // Drag Task A over Task B (which is in InProgress column)
    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(taskA.id));
    });
    act(() => {
      dndHandlers.onDragOver?.(makeDndEvent(taskA.id, taskB.id));
    });
    act(() => {
      dndHandlers.onDragEnd?.(makeDndEvent(taskA.id, taskB.id));
    });

    expect(onMoveTask).toHaveBeenCalledTimes(1);
    expect(onMoveTask).toHaveBeenCalledWith(taskA.id, inProgressCol.id, expect.any(Number));
  });
});
