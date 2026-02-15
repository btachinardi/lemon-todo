import { apiClient } from '@/lib/api-client';
import type {
  AddTagRequest,
  BulkCompleteRequest,
  CreateTaskRequest,
  ListTasksParams,
  MoveTaskRequest,
  PagedResult,
  UpdateTaskRequest,
} from '../types/api.types';
import type { Task } from '../types/task.types';

const BASE = '/api/tasks';

/**
 * Client for the tasks REST API (`/api/tasks`).
 * Each method maps 1:1 to a backend endpoint.
 *
 * @throws {@link import("@/lib/api-client").ApiRequestError} on non-2xx responses.
 */
export const tasksApi = {
  /** Paginated, filterable task list. */
  list(params?: ListTasksParams): Promise<PagedResult<Task>> {
    return apiClient.get<PagedResult<Task>>(BASE, params as Record<string, string | number | undefined>);
  },

  getById(id: string): Promise<Task> {
    return apiClient.get<Task>(`${BASE}/${id}`);
  },

  create(request: CreateTaskRequest): Promise<Task> {
    return apiClient.post<Task>(BASE, request);
  },

  update(id: string, request: UpdateTaskRequest): Promise<Task> {
    return apiClient.put<Task>(`${BASE}/${id}`, request);
  },

  /** Soft-deletes a task (sets `isDeleted` flag). */
  delete(id: string): Promise<void> {
    return apiClient.delete(`${BASE}/${id}`);
  },

  /** Transitions task status to `Done` and sets `completedAt`. */
  complete(id: string): Promise<void> {
    return apiClient.post(`${BASE}/${id}/complete`);
  },

  /** Reverts a completed task back to its previous status. */
  uncomplete(id: string): Promise<void> {
    return apiClient.post(`${BASE}/${id}/uncomplete`);
  },

  /** Sets `isArchived` flag -- hides the task from default views. */
  archive(id: string): Promise<void> {
    return apiClient.post(`${BASE}/${id}/archive`);
  },

  /**
   * Relocates a task card on the board. Also triggers a status transition
   * if the target column maps to a different {@link TaskStatus}.
   */
  move(id: string, request: MoveTaskRequest): Promise<void> {
    return apiClient.post(`${BASE}/${id}/move`, request);
  },

  addTag(id: string, request: AddTagRequest): Promise<void> {
    return apiClient.post(`${BASE}/${id}/tags`, request);
  },

  removeTag(id: string, tag: string): Promise<void> {
    return apiClient.delete(`${BASE}/${id}/tags/${encodeURIComponent(tag)}`);
  },

  bulkComplete(request: BulkCompleteRequest): Promise<void> {
    return apiClient.post(`${BASE}/bulk/complete`, request);
  },
};
