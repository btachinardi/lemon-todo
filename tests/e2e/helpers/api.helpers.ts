/**
 * Headless API helpers for seeding and mutating task/board data in E2E tests.
 *
 * All functions require an active auth session — call {@link createTestUser}
 * or {@link loginViaApi} in `beforeAll` before using any helper in this file.
 *
 * Every function throws on non-2xx API responses.
 *
 * @module
 */

import { getAuthToken } from './auth.helpers';
import { API_BASE } from './e2e.config';

interface CreateTaskRequest {
  title: string;
  description?: string | null;
  /** @defaultValue `'None'` on the server when omitted. */
  priority?: 'None' | 'Low' | 'Medium' | 'High' | 'Critical';
  /** ISO 8601 date string (e.g. `'2025-12-31T00:00:00Z'`), or `null` for no due date. */
  dueDate?: string | null;
  tags?: string[];
}

interface TaskResponse {
  id: string;
  title: string;
  status: string;
  priority: string;
  tags: string[];
}

interface CardResponse {
  taskId: string;
  columnId: string;
  rank: number;
}

interface BoardResponse {
  id: string;
  name: string;
  columns: { id: string; name: string; targetStatus: string; position: number; maxTasks: number | null }[];
  cards: CardResponse[];
}

async function authHeaders(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };
}

/**
 * Seeds a new task via the API. The task is automatically placed on the
 * user's default board in the "Todo" column.
 *
 * @throws If the API returns a non-2xx response.
 *
 * @example
 * const task = await createTask({ title: 'Buy groceries', priority: 'High' });
 * // task.id is now available for moveTask, completeTask, etc.
 */
export async function createTask(request: CreateTaskRequest): Promise<TaskResponse> {
  const res = await fetch(`${API_BASE}/tasks`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error(`Create task failed: ${res.status} ${await res.text()}`);
  return res.json();
}

/**
 * Marks a task as completed. The task's status changes to `'Completed'` and
 * its board card is moved to the "Done" column.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function completeTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/complete`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Complete task failed: ${res.status}`);
}

/**
 * Reverts a completed task back to `'Todo'` status and moves its board card
 * to the "Todo" column.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function uncompleteTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/uncomplete`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Uncomplete task failed: ${res.status}`);
}

/**
 * Fetches the current user's default board, including columns and card placements.
 * Every user has exactly one default board created at registration.
 *
 * Use the returned `columns` to resolve column IDs before calling {@link moveTask}.
 * The `cards` array contains only active (non-archived, non-deleted) task cards.
 *
 * @throws If the API returns a non-2xx response.
 *
 * @example
 * const board = await getDefaultBoard();
 * const inProgressCol = board.columns.find(c => c.targetStatus === 'InProgress')!;
 */
export async function getDefaultBoard(): Promise<BoardResponse> {
  const res = await fetch(`${API_BASE}/boards/default`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Get default board failed: ${res.status}`);
  return res.json();
}

/**
 * Permanently deletes a task. Also removes its board card. This is
 * irreversible — prefer {@link archiveTask} for non-destructive removal.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function deleteTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}`, {
    method: 'DELETE',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Delete task failed: ${res.status}`);
}

/**
 * Lists all active (non-archived) tasks for the current user.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function listTasks(): Promise<{ items: TaskResponse[] }> {
  const res = await fetch(`${API_BASE}/tasks`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`List tasks failed: ${res.status}`);
  return res.json();
}

/**
 * Moves a task card to a position within a column on the default board.
 *
 * Pass `null` for `previousTaskId` to place the card at the **top** of the column.
 * Pass `null` for `nextTaskId` to place the card at the **bottom** of the column.
 * Pass both as `null` when moving to an empty column.
 *
 * **Side effect**: Cross-column moves also update the task's status to match
 * the target column's `targetStatus` (e.g. moving to "In Progress" sets the
 * task status to `InProgress`).
 *
 * @param id - The task ID to move.
 * @param columnId - Target column ID. Obtain from {@link getDefaultBoard}`().columns`.
 * @param previousTaskId - Task that should appear **above** the moved card, or `null` for top.
 * @param nextTaskId - Task that should appear **below** the moved card, or `null` for bottom.
 * @throws If the API returns a non-2xx response.
 *
 * @example
 * const board = await getDefaultBoard();
 * const todoCol = board.columns.find(c => c.targetStatus === 'Todo')!;
 * // Place taskC between taskA (above) and taskB (below)
 * await moveTask(taskC.id, todoCol.id, taskA.id, taskB.id);
 * // Place taskD at the top of the column
 * await moveTask(taskD.id, todoCol.id, null, taskA.id);
 */
export async function moveTask(
  id: string,
  columnId: string,
  previousTaskId: string | null,
  nextTaskId: string | null,
): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/move`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify({ columnId, previousTaskId, nextTaskId }),
  });
  if (!res.ok) throw new Error(`Move task failed: ${res.status} ${await res.text()}`);
}

/**
 * Archives a task, removing it from the board without deleting the record.
 * Archived tasks no longer appear in {@link listTasks} or {@link getDefaultBoard}
 * card results.
 *
 * @throws If the API returns a non-2xx response.
 */
export async function archiveTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/archive`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Archive task failed: ${res.status}`);
}
