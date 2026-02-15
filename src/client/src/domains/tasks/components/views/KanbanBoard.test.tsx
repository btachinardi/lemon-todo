import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import type { ReactNode } from 'react';
import type {
  DndContextProps,
  DragStartEvent,
  DragOverEvent,
  DragEndEvent,
  DragCancelEvent,
} from '@dnd-kit/core';
import { KanbanBoard } from './KanbanBoard';
import { createBoard, createTask, createTaskCard } from '@/test/factories';
import { TaskStatus } from '../../types/task.types';

/**
 * Captured DndContext event handlers. Updated on every render of KanbanBoard
 * because the mock DndContext re-captures props each time.
 */
let dndHandlers: {
  onDragStart?: (event: DragStartEvent) => void;
  onDragOver?: (event: DragOverEvent) => void;
  onDragEnd?: (event: DragEndEvent) => void;
  onDragCancel?: (event: DragCancelEvent) => void;
};

// Mock @dnd-kit/core — capture DndContext handlers so we can invoke them directly
vi.mock('@dnd-kit/core', () => ({
  DndContext: ({ children, onDragStart, onDragOver, onDragEnd, onDragCancel }: DndContextProps) => {
    dndHandlers = { onDragStart, onDragOver, onDragEnd, onDragCancel };
    return <>{children}</>;
  },
  DragOverlay: ({ children }: { children?: ReactNode }) => <>{children}</>,
  closestCorners: vi.fn(),
  PointerSensor: class {},
  useSensor: () => ({}),
  useSensors: () => [],
  useDroppable: () => ({ setNodeRef: vi.fn(), isOver: false }),
}));

// Mock @dnd-kit/sortable — provide inert implementations
vi.mock('@dnd-kit/sortable', () => ({
  SortableContext: ({ children }: { children: ReactNode }) => <>{children}</>,
  verticalListSortingStrategy: {},
  useSortable: () => ({
    attributes: { role: 'button', tabIndex: 0 },
    listeners: {},
    setNodeRef: vi.fn(),
    transform: null,
    transition: null,
    isDragging: false,
  }),
  arrayMove: <T,>(arr: T[], from: number, to: number): T[] => {
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
    collisions: [],
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
      createTaskCard({ taskId: task1.id, columnId: todoColumnId, rank: 1000 }),
      createTaskCard({ taskId: task2.id, columnId: doneColumnId, rank: 1000 }),
    ];

    render(<KanbanBoard board={board} tasks={[task1, task2]} />);
    expect(screen.getByText('Task in Todo')).toBeInTheDocument();
    expect(screen.getByText('Task in Done')).toBeInTheDocument();
  });

  it('sorts tasks within columns by rank', () => {
    const board = createBoard();
    const todoColumnId = board.columns[0].id;

    const task1 = createTask({ title: 'Third (rank 3000)' });
    const task2 = createTask({ title: 'First (rank 1000)' });
    const task3 = createTask({ title: 'Second (rank 2000)' });

    board.cards = [
      createTaskCard({ taskId: task1.id, columnId: todoColumnId, rank: 3000 }),
      createTaskCard({ taskId: task2.id, columnId: todoColumnId, rank: 1000 }),
      createTaskCard({ taskId: task3.id, columnId: todoColumnId, rank: 2000 }),
    ];

    render(<KanbanBoard board={board} tasks={[task1, task2, task3]} />);

    // Cards should appear in rank order: First, Second, Third
    // Each TaskCard has aria-label="Task: {title}"
    const cards = screen.getAllByRole('button', { name: /^Task:/ });
    const labels = cards.map((el) => el.getAttribute('aria-label'));

    expect(labels[0]).toContain('First');
    expect(labels[1]).toContain('Second');
    expect(labels[2]).toContain('Third');
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

  it('should call onMoveTask with neighbor IDs when card is dragged to a different column', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];
    const inProgressCol = board.columns[1];

    const task = createTask({ title: 'Drag me' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: todoCol.id, rank: 1000 })];

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

    // onMoveTask MUST be called with the target column and neighbor IDs
    expect(onMoveTask).toHaveBeenCalledTimes(1);
    expect(onMoveTask).toHaveBeenCalledWith(
      task.id,
      inProgressCol.id,
      null, // previousTaskId — empty column, no card above
      null, // nextTaskId — empty column, no card below
    );
  });

  it('should call onMoveTask with correct neighbors when dropping onto a task in another column', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];
    const inProgressCol = board.columns[1];

    const taskA = createTask({ title: 'Task A' });
    const taskB = createTask({ title: 'Task B' });
    board.cards = [
      createTaskCard({ taskId: taskA.id, columnId: todoCol.id, rank: 1000 }),
      createTaskCard({ taskId: taskB.id, columnId: inProgressCol.id, rank: 1000 }),
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
    // Dropping on Task B places A at B's index (before B), consistent with arrayMove
    expect(onMoveTask).toHaveBeenCalledWith(
      taskA.id,
      inProgressCol.id,
      null,     // previousTaskId — A is at top (B's position)
      taskB.id, // nextTaskId — B shifted down
    );
  });

  it('should send correct neighbor IDs for same-column reorder', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];

    const taskA = createTask({ title: 'Task A' });
    const taskB = createTask({ title: 'Task B' });
    const taskC = createTask({ title: 'Task C' });
    board.cards = [
      createTaskCard({ taskId: taskA.id, columnId: todoCol.id, rank: 1000 }),
      createTaskCard({ taskId: taskB.id, columnId: todoCol.id, rank: 2000 }),
      createTaskCard({ taskId: taskC.id, columnId: todoCol.id, rank: 3000 }),
    ];

    render(
      <KanbanBoard board={board} tasks={[taskA, taskB, taskC]} onMoveTask={onMoveTask} />,
    );

    // Drag C to position between A and B (same column)
    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(taskC.id));
    });
    act(() => {
      dndHandlers.onDragEnd?.(makeDndEvent(taskC.id, taskA.id));
    });

    expect(onMoveTask).toHaveBeenCalledTimes(1);
    // C dropped onto A's position → arrayMove puts C at index 0 (top)
    // Order becomes [C, A, B] so C has no previous, next is A
    expect(onMoveTask).toHaveBeenCalledWith(
      taskC.id,
      todoCol.id,
      null,      // previousTaskId — C is now at top
      taskA.id,  // nextTaskId — A is next after C
    );
  });

  it('should use drop target position when cross-column drop differs from initial hover', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];
    const inProgressCol = board.columns[1];

    const taskA = createTask({ title: 'Task A' });
    const taskP = createTask({ title: 'Task P', status: TaskStatus.InProgress });
    const taskQ = createTask({ title: 'Task Q', status: TaskStatus.InProgress });
    const taskR = createTask({ title: 'Task R', status: TaskStatus.InProgress });
    board.cards = [
      createTaskCard({ taskId: taskA.id, columnId: todoCol.id, rank: 1000 }),
      createTaskCard({ taskId: taskP.id, columnId: inProgressCol.id, rank: 1000 }),
      createTaskCard({ taskId: taskQ.id, columnId: inProgressCol.id, rank: 2000 }),
      createTaskCard({ taskId: taskR.id, columnId: inProgressCol.id, rank: 3000 }),
    ];

    render(
      <KanbanBoard
        board={board}
        tasks={[taskA, taskP, taskQ, taskR]}
        onMoveTask={onMoveTask}
      />,
    );

    // 1. Start dragging Task A from Todo
    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(taskA.id));
    });

    // 2. Drag over Task P (enters InProgress column — handleDragOver inserts A after P)
    act(() => {
      dndHandlers.onDragOver?.(makeDndEvent(taskA.id, taskP.id));
    });

    // 3. Drop on Task R (user dragged further down — actual drop target is R, not P)
    act(() => {
      dndHandlers.onDragEnd?.(makeDndEvent(taskA.id, taskR.id));
    });

    // A should land at R's position (after Q, before R or after R depending on arrayMove)
    // The key assertion: neighbors should be relative to R's position, NOT P's position
    expect(onMoveTask).toHaveBeenCalledTimes(1);
    const [, columnId, previousTaskId] = onMoveTask.mock.calls[0];
    expect(columnId).toBe(inProgressCol.id);
    // A was dropped on R → arrayMove moves A from index 1 (after P) to index 3 (R's position)
    // Final order: [P, Q, R, A] → previous=R, next=null
    expect(previousTaskId).toBe(taskR.id);
  });

  it('should compute correct neighbors when cross-column drop lands at top of non-empty column', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];
    const inProgressCol = board.columns[1];

    const taskA = createTask({ title: 'Task A' });
    const taskP = createTask({ title: 'Task P', status: TaskStatus.InProgress });
    const taskQ = createTask({ title: 'Task Q', status: TaskStatus.InProgress });
    board.cards = [
      createTaskCard({ taskId: taskA.id, columnId: todoCol.id, rank: 1000 }),
      createTaskCard({ taskId: taskP.id, columnId: inProgressCol.id, rank: 1000 }),
      createTaskCard({ taskId: taskQ.id, columnId: inProgressCol.id, rank: 2000 }),
    ];

    render(
      <KanbanBoard board={board} tasks={[taskA, taskP, taskQ]} onMoveTask={onMoveTask} />,
    );

    // Drag A into InProgress via Q (enters near bottom), then drop on P (top)
    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(taskA.id));
    });
    act(() => {
      dndHandlers.onDragOver?.(makeDndEvent(taskA.id, taskQ.id));
    });
    act(() => {
      dndHandlers.onDragEnd?.(makeDndEvent(taskA.id, taskP.id));
    });

    expect(onMoveTask).toHaveBeenCalledTimes(1);
    // A entered after Q → columnItems: [P, Q, A]
    // Drop on P → arrayMove from index 2 to index 0 → [A, P, Q]
    // Neighbors: previous=null, next=P
    expect(onMoveTask).toHaveBeenCalledWith(
      taskA.id,
      inProgressCol.id,
      null,      // previousTaskId — top of column
      taskP.id,  // nextTaskId
    );
  });

  it('should send previousTaskId=null when dragged to top of column', () => {
    const onMoveTask = vi.fn();
    const board = createBoard();
    const todoCol = board.columns[0];

    const taskA = createTask({ title: 'Task A' });
    const taskB = createTask({ title: 'Task B' });
    board.cards = [
      createTaskCard({ taskId: taskA.id, columnId: todoCol.id, rank: 1000 }),
      createTaskCard({ taskId: taskB.id, columnId: todoCol.id, rank: 2000 }),
    ];

    render(
      <KanbanBoard board={board} tasks={[taskA, taskB]} onMoveTask={onMoveTask} />,
    );

    // Drag B above A (to the top)
    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(taskB.id));
    });
    act(() => {
      // Drop onto position 0 (where A was) — arrayMove puts B before A
      dndHandlers.onDragEnd?.(makeDndEvent(taskB.id, taskA.id));
    });

    expect(onMoveTask).toHaveBeenCalledTimes(1);
    // B is now at index 0: previousTaskId=null, nextTaskId=A
    expect(onMoveTask).toHaveBeenCalledWith(
      taskB.id,
      todoCol.id,
      null,      // previousTaskId — top of column
      taskA.id,  // nextTaskId
    );
  });
});
