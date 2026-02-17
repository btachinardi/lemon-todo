import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import type { ReactNode } from 'react';
import type {
  DndContextProps,
  DragStartEvent,
  DragMoveEvent,
  DragOverEvent,
  DragEndEvent,
  DragCancelEvent,
  AutoScrollOptions,
} from '@dnd-kit/core';
import { KanbanBoard } from './KanbanBoard';
import { createBoard, createTask, createTaskCard } from '@/test/factories';
import { TaskStatus } from '../../types/task.types';

/** Spy on useSensor calls to verify sensor configuration. */
const mockUseSensor = vi.hoisted(() => vi.fn<(sensor: { name: string }, options?: Record<string, unknown>) => Record<string, unknown>>());

/**
 * Captured DndContext event handlers. Updated on every render of KanbanBoard
 * because the mock DndContext re-captures props each time.
 */
let dndHandlers: {
  onDragStart?: (event: DragStartEvent) => void;
  onDragMove?: (event: DragMoveEvent) => void;
  onDragOver?: (event: DragOverEvent) => void;
  onDragEnd?: (event: DragEndEvent) => void;
  onDragCancel?: (event: DragCancelEvent) => void;
};

/** Captured autoScroll prop from DndContext. */
let dndAutoScroll: boolean | AutoScrollOptions | undefined;

// Mock @dnd-kit/core — capture DndContext handlers so we can invoke them directly
vi.mock('@dnd-kit/core', () => ({
  DndContext: ({ children, autoScroll, onDragStart, onDragMove, onDragOver, onDragEnd, onDragCancel }: DndContextProps) => {
    dndHandlers = { onDragStart, onDragMove, onDragOver, onDragEnd, onDragCancel };
    dndAutoScroll = autoScroll;
    return <>{children}</>;
  },
  DragOverlay: ({ children }: { children?: ReactNode }) => <>{children}</>,
  closestCorners: vi.fn(),
  PointerSensor: class PointerSensor {},
  TouchSensor: class TouchSensor {},
  KeyboardSensor: class KeyboardSensor {},
  useSensor: mockUseSensor,
  useSensors: (...sensors: unknown[]) => sensors,
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

/** Creates a DnD event with a mock touch activator for mobile drag testing. */
function makeTouchDndEvent(activeId: string, initialClientX: number, deltaX: number, overId?: string | null) {
  const mockRect = { top: 0, left: 0, right: 0, bottom: 0, width: 0, height: 0, x: 0, y: 0 };
  const touchEvent = new Event('touchstart');
  Object.defineProperty(touchEvent, 'touches', {
    value: [{ clientX: initialClientX, clientY: 100 }],
  });
  return {
    active: { id: activeId, data: { current: {} }, rect: { current: { initial: mockRect, translated: mockRect } } },
    over: overId != null
      ? { id: overId, data: { current: {} }, rect: mockRect, disabled: false }
      : null,
    collisions: [],
    delta: { x: deltaX, y: 0 },
    activatorEvent: touchEvent,
  };
}

/** Sets up mock DOM measurements on the scroll container and its column children. */
function setupScrollContainerMocks(container: Element) {
  const scrollContainer = container.querySelector('.overflow-x-auto')!;
  const mockScrollTo = vi.fn();
  scrollContainer.scrollTo = mockScrollTo;
  Object.defineProperty(scrollContainer, 'clientWidth', { value: 375, configurable: true });
  Object.defineProperty(scrollContainer, 'scrollLeft', { value: 0, configurable: true, writable: true });

  // 3 columns: each ~319px wide, 16px gaps, 16px padding
  const flexContainer = scrollContainer.firstElementChild!;
  for (let i = 0; i < flexContainer.children.length; i++) {
    Object.defineProperty(flexContainer.children[i], 'offsetLeft', { value: 16 + i * 335, configurable: true });
    Object.defineProperty(flexContainer.children[i], 'offsetWidth', { value: 319, configurable: true });
  }

  return { scrollContainer, mockScrollTo };
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

describe('KanbanBoard mobile touch support', () => {
  beforeEach(() => {
    mockUseSensor.mockClear();
    dndHandlers = {};
  });

  it('should configure TouchSensor for mobile drag support', () => {
    const board = createBoard();
    render(<KanbanBoard board={board} tasks={[]} />);

    const sensorNames = mockUseSensor.mock.calls.map((call) => call[0]?.name);
    expect(sensorNames).toContain('TouchSensor');
  });

  it('should configure TouchSensor with delay activation to avoid conflicting with scroll', () => {
    const board = createBoard();
    render(<KanbanBoard board={board} tasks={[]} />);

    const touchSensorCall = mockUseSensor.mock.calls.find(
      (call) => call[0]?.name === 'TouchSensor',
    );
    expect(touchSensorCall).toBeDefined();

    const options = touchSensorCall![1] as { activationConstraint?: { delay?: number; tolerance?: number } };
    // TouchSensor should use delay-based activation (not just distance) so
    // a quick swipe scrolls normally while a press-and-hold initiates drag
    expect(options?.activationConstraint?.delay).toBeGreaterThanOrEqual(200);
    expect(options?.activationConstraint?.tolerance).toBeGreaterThanOrEqual(5);
  });

  it('should apply snap scroll classes on the scroll container (overflow element), not inner flex', () => {
    const board = createBoard();
    const task = createTask({ title: 'Snap container test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const scrollContainer = container.querySelector('.overflow-x-auto')!;
    const innerFlex = container.querySelector('[data-onboarding="board-columns"]')!;

    // snap-x and snap-mandatory MUST be on the scroll container for CSS scroll snapping to work
    expect(scrollContainer.className).toContain('snap-x');
    expect(scrollContainer.className).toContain('snap-mandatory');

    // inner flex container should NOT have snap classes (they have no effect there)
    expect(innerFlex.className).not.toContain('snap-x');
    expect(innerFlex.className).not.toContain('snap-mandatory');
  });

  it('should disable snap scroll during active drag to prevent layout jumps', () => {
    const board = createBoard();
    const task = createTask({ title: 'Snap test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const scrollContainer = container.querySelector('.overflow-x-auto')!;

    // Before drag: snap scroll should be active on the scroll container
    expect(scrollContainer.className).toContain('snap-x');
    expect(scrollContainer.className).toContain('snap-mandatory');

    // During drag: snap must be disabled so dropping doesn't cause layout jump
    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(task.id));
    });
    expect(scrollContainer.className).not.toContain('snap-x');
    expect(scrollContainer.className).not.toContain('snap-mandatory');

    // After drag ends: snap should be restored
    act(() => {
      dndHandlers.onDragEnd?.(makeDndEvent(task.id, task.id));
    });
    expect(scrollContainer.className).toContain('snap-x');
    expect(scrollContainer.className).toContain('snap-mandatory');
  });

  it('should restore snap scroll on drag cancel', () => {
    const board = createBoard();
    const task = createTask({ title: 'Cancel test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const scrollContainer = container.querySelector('.overflow-x-auto')!;

    act(() => {
      dndHandlers.onDragStart?.(makeDndEvent(task.id));
    });
    expect(scrollContainer.className).not.toContain('snap-x');

    act(() => {
      dndHandlers.onDragCancel?.({
        ...makeDndEvent(task.id),
        over: null,
      } as unknown as DragCancelEvent);
    });
    expect(scrollContainer.className).toContain('snap-x');
    expect(scrollContainer.className).toContain('snap-mandatory');
  });
});

describe('KanbanBoard mobile column-snap auto-scroll', () => {
  let originalInnerWidth: number;

  beforeEach(() => {
    dndHandlers = {};
    dndAutoScroll = undefined;
    originalInnerWidth = window.innerWidth;
    Object.defineProperty(window, 'innerWidth', { value: 375, configurable: true, writable: true });
  });

  afterEach(() => {
    Object.defineProperty(window, 'innerWidth', { value: originalInnerWidth, configurable: true, writable: true });
  });

  it('should configure autoScroll with canScroll to skip horizontal board container', () => {
    const board = createBoard();
    render(<KanbanBoard board={board} tasks={[]} />);

    expect(dndAutoScroll).toBeDefined();
    expect(typeof dndAutoScroll).toBe('object');
    expect(typeof (dndAutoScroll as AutoScrollOptions).canScroll).toBe('function');
  });

  it('should provide onDragMove handler for column-snap scrolling', () => {
    const board = createBoard();
    render(<KanbanBoard board={board} tasks={[]} />);

    expect(dndHandlers.onDragMove).toBeDefined();
    expect(typeof dndHandlers.onDragMove).toBe('function');
  });

  it('should scroll to next column when pointer reaches right edge during drag on mobile', () => {
    const board = createBoard();
    const task = createTask({ title: 'Snap right test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { mockScrollTo } = setupScrollContainerMocks(container);

    // Start drag from center of screen (touch at x=200)
    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Move pointer to right edge zone (200 + 120 = 320 > 375 - 60 = 315)
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });

    expect(mockScrollTo).toHaveBeenCalledTimes(1);
    expect(mockScrollTo).toHaveBeenCalledWith(expect.objectContaining({ behavior: 'smooth' }));
  });

  it('should scroll to previous column when pointer reaches left edge during drag on mobile', () => {
    const board = createBoard();
    const task = createTask({ title: 'Snap left test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { scrollContainer, mockScrollTo } = setupScrollContainerMocks(container);

    // Simulate being scrolled to column 1
    Object.defineProperty(scrollContainer, 'scrollLeft', { value: 335, configurable: true, writable: true });

    // Start drag from center (touch at x=200)
    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Move pointer to left edge zone (200 + (-170) = 30 < 60)
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, -170) as unknown as DragMoveEvent);
    });

    expect(mockScrollTo).toHaveBeenCalledTimes(1);
    expect(mockScrollTo).toHaveBeenCalledWith(expect.objectContaining({ behavior: 'smooth' }));
  });

  it('should not scroll again during cooldown period', () => {
    const board = createBoard();
    const task = createTask({ title: 'Cooldown test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { mockScrollTo } = setupScrollContainerMocks(container);

    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // First move to right edge → should scroll
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });

    // Second move immediately (no time passed) → should NOT scroll due to cooldown
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 130) as unknown as DragMoveEvent);
    });

    expect(mockScrollTo).toHaveBeenCalledTimes(1);
  });

  it('should not auto-scroll on desktop viewports', () => {
    Object.defineProperty(window, 'innerWidth', { value: 1024, configurable: true, writable: true });

    const board = createBoard();
    const task = createTask({ title: 'Desktop test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { mockScrollTo } = setupScrollContainerMocks(container);

    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Move to right edge zone — should be ignored on desktop
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 800) as unknown as DragMoveEvent);
    });

    expect(mockScrollTo).not.toHaveBeenCalled();
  });

  it('should not scroll past the last column', () => {
    const board = createBoard();
    const task = createTask({ title: 'Boundary test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[2].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { scrollContainer, mockScrollTo } = setupScrollContainerMocks(container);

    // Already at the last column (column index 2)
    Object.defineProperty(scrollContainer, 'scrollLeft', { value: 670, configurable: true, writable: true });

    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Move to right edge — already at last column, should not scroll
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });

    expect(mockScrollTo).not.toHaveBeenCalled();
  });

  it('should not re-trigger same edge zone snap until pointer leaves the zone', () => {
    const board = createBoard();
    const task = createTask({ title: 'Re-entry test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { mockScrollTo } = setupScrollContainerMocks(container);

    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // First move to right edge → snap fires
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    // Advance past cooldown but pointer STAYS in right edge zone → should NOT snap again
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 1000);
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 125) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1); // still 1, no re-trigger

    vi.restoreAllMocks();
  });

  it('should allow reverse-direction snap by moving pointer back from the snap position', () => {
    const board = createBoard();
    const task = createTask({ title: 'Reverse snap test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { scrollContainer, mockScrollTo } = setupScrollContainerMocks(container);

    // Start scrolled at column 1 so there's room to go right then back left
    Object.defineProperty(scrollContainer, 'scrollLeft', { value: 335, configurable: true, writable: true });

    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Snap right via edge zone
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    // Advance past cooldown, then drag left by ≥80px from the snap position (320 → 230)
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 1000);
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 30) as unknown as DragMoveEvent);
    });

    // Should have snapped left (2 total scrolls: one right, one left)
    expect(mockScrollTo).toHaveBeenCalledTimes(2);

    vi.restoreAllMocks();
  });

  it('should not trigger distance-based snap when movement is less than snap distance threshold', () => {
    const board = createBoard();
    const task = createTask({ title: 'Below-threshold test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { scrollContainer, mockScrollTo } = setupScrollContainerMocks(container);

    Object.defineProperty(scrollContainer, 'scrollLeft', { value: 335, configurable: true, writable: true });

    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Snap right
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    // Advance past cooldown, move left but only 50px from snap point (320 → 270)
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 1000);
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 70) as unknown as DragMoveEvent);
    });

    // Should NOT have snapped — still only 1 scroll
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    vi.restoreAllMocks();
  });

  it('should support a full right-then-left snap sequence in a single drag', () => {
    const board = createBoard();
    const task = createTask({ title: 'Full sequence test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { mockScrollTo } = setupScrollContainerMocks(container);

    // Start at column 0
    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // 1. Snap RIGHT: drag to right edge (pointerX = 200+120 = 320 > 375-60 = 315)
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    // 2. Advance time, move slightly left but stay within snap distance threshold
    //    Snap anchor is at 320. Move to 260 (60px away, below 80px threshold).
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 1000);
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 60) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1); // no snap, within threshold

    // 3. Snap LEFT: continue dragging left (pointerX = 200-120 = 80)
    //    That's 240px left of the snap anchor (320), well above 80px threshold
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 2000);
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, -120) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(2); // one right + one left

    vi.restoreAllMocks();
  });

  it('should block snaps within the 800ms cooldown window', () => {
    const board = createBoard();
    const task = createTask({ title: 'Extended cooldown test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { scrollContainer, mockScrollTo } = setupScrollContainerMocks(container);

    // Start at column 1 so there's room to snap right then back left
    Object.defineProperty(scrollContainer, 'scrollLeft', { value: 335, configurable: true, writable: true });

    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Snap right via edge zone
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    // Advance 500ms — past old 400ms cooldown but within new 800ms cooldown
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 500);

    // Try to snap left via distance (pointerX=80, dx=80-320=-240, well past threshold)
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, -120) as unknown as DragMoveEvent);
    });

    // Should NOT have snapped — still within 800ms cooldown
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    vi.restoreAllMocks();
  });

  it('should use raw pointer position from native pointermove events for edge zone detection', () => {
    const board = createBoard();
    const task = createTask({ title: 'Raw pointer test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { mockScrollTo } = setupScrollContainerMocks(container);

    // Start drag at x=200
    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Simulate a native pointermove at right edge zone (x=320)
    // while dnd-kit delta reports 0 (e.g., scroll compensation ate the movement)
    act(() => {
      window.dispatchEvent(new PointerEvent('pointermove', { clientX: 320 }));
    });

    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 0) as unknown as DragMoveEvent);
    });

    // Raw pointer at 320 > 315 (right edge zone) → should snap right
    // Delta-based: pointerX = 200+0 = 200 → NOT in edge zone → would NOT snap
    expect(mockScrollTo).toHaveBeenCalledTimes(1);
  });

  it('should not trigger reverse snap when pointer stays still but delta shifts from scroll compensation', () => {
    const board = createBoard();
    const task = createTask({ title: 'No oscillation test' });
    board.cards = [createTaskCard({ taskId: task.id, columnId: board.columns[0].id, rank: 1000 })];

    const { container } = render(<KanbanBoard board={board} tasks={[task]} />);
    const { mockScrollTo } = setupScrollContainerMocks(container);

    // Start drag at x=200
    act(() => {
      dndHandlers.onDragStart?.(makeTouchDndEvent(task.id, 200, 0));
    });

    // Move pointer to right edge and snap
    act(() => {
      window.dispatchEvent(new PointerEvent('pointermove', { clientX: 320 }));
    });
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, 120) as unknown as DragMoveEvent);
    });
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    // After cooldown, finger hasn't moved (still at viewport x=320)
    // but dnd-kit delta shifted due to scroll compensation: 120 - 335 = -215
    vi.spyOn(Date, 'now').mockReturnValue(Date.now() + 1000);
    act(() => {
      window.dispatchEvent(new PointerEvent('pointermove', { clientX: 320 }));
    });
    act(() => {
      dndHandlers.onDragMove?.(makeTouchDndEvent(task.id, 200, -215) as unknown as DragMoveEvent);
    });

    // Should NOT have reverse-snapped — finger is still at 320, no real movement
    // With raw tracking: pointerX=320, snapAnchor=320, dx=0 → no snap ✓
    // Without raw tracking: pointerX=200+(-215)=-15, dx=-15-320=-335 → snap left ✗
    expect(mockScrollTo).toHaveBeenCalledTimes(1);

    vi.restoreAllMocks();
  });
});

describe('KanbanBoard mobile height stretching', () => {
  it('should apply min-h-full on inner flex container so columns stretch to fill viewport', () => {
    const board = createBoard();
    const { container } = render(<KanbanBoard board={board} tasks={[]} />);
    const innerFlex = container.querySelector('[data-onboarding="board-columns"]')!;

    expect(innerFlex.className).toContain('min-h-full');
  });
});
