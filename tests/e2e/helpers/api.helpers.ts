const API_BASE = 'http://localhost:5155/api';

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

interface BoardResponse {
  id: string;
  name: string;
  columns: { id: string; name: string; targetStatus: string; position: number; maxTasks: number | null }[];
}

export async function createTask(request: CreateTaskRequest): Promise<TaskResponse> {
  const res = await fetch(`${API_BASE}/tasks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  });
  if (!res.ok) throw new Error(`Create task failed: ${res.status} ${await res.text()}`);
  return res.json();
}

export async function completeTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/complete`, { method: 'POST' });
  if (!res.ok) throw new Error(`Complete task failed: ${res.status}`);
}

export async function uncompleteTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}/uncomplete`, { method: 'POST' });
  if (!res.ok) throw new Error(`Uncomplete task failed: ${res.status}`);
}

export async function getDefaultBoard(): Promise<BoardResponse> {
  const res = await fetch(`${API_BASE}/boards/default`);
  if (!res.ok) throw new Error(`Get default board failed: ${res.status}`);
  return res.json();
}

export async function deleteTask(id: string): Promise<void> {
  const res = await fetch(`${API_BASE}/tasks/${id}`, { method: 'DELETE' });
  if (!res.ok) throw new Error(`Delete task failed: ${res.status}`);
}

export async function listTasks(): Promise<{ items: TaskResponse[] }> {
  const res = await fetch(`${API_BASE}/tasks`);
  if (!res.ok) throw new Error(`List tasks failed: ${res.status}`);
  return res.json();
}

/** Delete all tasks to reset state between tests. */
export async function deleteAllTasks(): Promise<void> {
  const { items } = await listTasks();
  for (const task of items) {
    await deleteTask(task.id);
  }
}
