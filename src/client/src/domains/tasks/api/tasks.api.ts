import { apiClient } from '@/lib/api-client';
import type {
  AddTagRequest,
  BulkCompleteRequest,
  CreateTaskRequest,
  ListTasksParams,
  MoveTaskRequest,
  PagedResult,
  UpdateTaskRequest,
  ViewTaskNoteRequest,
  ViewTaskNoteResponse,
} from '../types/api.types';
import type { Task } from '../types/task.types';

const BASE = '/api/tasks';

/**
 * Client for the tasks REST API (`/api/tasks`).
 * Each method maps 1:1 to a backend endpoint.
 *
 * @throws {@link import("@/lib/api-client").ApiRequestError} on non-2xx responses:
 * - 400 validation errors (malformed request, constraint violations)
 * - 401 unauthorized (missing or invalid authentication)
 * - 404 not found (task does not exist or was deleted)
 */
export const tasksApi = {
  /** Paginated, filterable task list. */
  list(params?: ListTasksParams): Promise<PagedResult<Task>> {
    return apiClient.get<PagedResult<Task>>(BASE, params as Record<string, string | number | boolean | null | undefined>);
  },

  /** Fetches a single task by ID. Returns 404 if the task is deleted. */
  getById(id: string): Promise<Task> {
    return apiClient.get<Task>(`${BASE}/${id}`);
  },

  /** Creates a new task and places it on the default board. */
  create(request: CreateTaskRequest): Promise<Task> {
    return apiClient.post<Task>(BASE, request);
  },

  /** Partial-updates a task. Only provided fields are modified (patch semantics). */
  update(id: string, request: UpdateTaskRequest): Promise<Task> {
    return apiClient.put<Task>(`${BASE}/${id}`, request);
  },

  /** Soft-deletes a task (sets `isDeleted` flag). */
  delete(id: string): Promise<void> {
    return apiClient.deleteVoid(`${BASE}/${id}`);
  },

  /** Transitions task status to `Done` and sets `completedAt`. */
  complete(id: string): Promise<void> {
    return apiClient.postVoid(`${BASE}/${id}/complete`);
  },

  /** Reverts a completed task back to its previous status. */
  uncomplete(id: string): Promise<void> {
    return apiClient.postVoid(`${BASE}/${id}/uncomplete`);
  },

  /** Sets `isArchived` flag -- hides the task from default views. */
  archive(id: string): Promise<void> {
    return apiClient.postVoid(`${BASE}/${id}/archive`);
  },

  /**
   * Relocates a task card on the board. Also triggers a status transition
   * if the target column maps to a different {@link TaskStatus}.
   */
  move(id: string, request: MoveTaskRequest): Promise<void> {
    return apiClient.postVoid(`${BASE}/${id}/move`, request);
  },

  /** Appends a tag to a task. Silently succeeds if the tag already exists. */
  addTag(id: string, request: AddTagRequest): Promise<void> {
    return apiClient.postVoid(`${BASE}/${id}/tags`, request);
  },

  /** Removes a tag from a task. The tag value is URI-encoded for safe transport. */
  removeTag(id: string, tag: string): Promise<void> {
    return apiClient.deleteVoid(`${BASE}/${id}/tags/${encodeURIComponent(tag)}`);
  },

  /** Decrypts and returns the task's sensitive note after password re-authentication. */
  viewNote(id: string, request: ViewTaskNoteRequest): Promise<ViewTaskNoteResponse> {
    return apiClient.post<ViewTaskNoteResponse>(`${BASE}/${id}/view-note`, request);
  },

  /** Marks multiple tasks as complete in a single request. */
  bulkComplete(request: BulkCompleteRequest): Promise<void> {
    return apiClient.postVoid(`${BASE}/bulk/complete`, request);
  },
};
