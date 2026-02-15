import { getAuthToken } from './auth.helpers';
import { API_BASE } from './e2e.config';

interface CreateTaskRequest {
  title: string;
  description?: string | null;
  priority?: 'None' | 'Low' | 'Medium' | 'High' | 'Critical';
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

/** Builds request headers with auth token. */
async function authHeaders(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`,
  };
}

export async function createTask(request: CreateTaskRequest): Promise<TaskResponse> {
  const res = await fetch(`${API_BASE}/tasks`, {
    method: 'POST',
    headers: await authHeaders(),
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error(`Create task failed: ${res.status} ${await res.text()}`);
  return res.json();
}

export async function completeTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/complete`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Complete task failed: ${res.status}`);
}

export async function uncompleteTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/uncomplete`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Uncomplete task failed: ${res.status}`);
}

export async function getDefaultBoard(): Promise<BoardResponse> {
  const res = await fetch(`${API_BASE}/boards/default`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Get default board failed: ${res.status}`);
  return res.json();
}

export async function deleteTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}`, {
    method: 'DELETE',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Delete task failed: ${res.status}`);
}

export async function listTasks(): Promise<{ items: TaskResponse[] }> {
  const res = await fetch(`${API_BASE}/tasks`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`List tasks failed: ${res.status}`);
  return res.json();
}

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

export async function archiveTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/archive`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Archive task failed: ${res.status}`);
}

