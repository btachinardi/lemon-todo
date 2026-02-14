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
import type { BoardTask } from '../types/task.types';

const BASE = '/api/tasks';

export const tasksApi = {
  list(params?: ListTasksParams): Promise<PagedResult<BoardTask>> {
    return apiClient.get<PagedResult<BoardTask>>(BASE, params as Record<string, string | number | undefined>);
  },

  getById(id: string): Promise<BoardTask> {
    return apiClient.get<BoardTask>(`${BASE}/${id}`);
  },

  create(request: CreateTaskRequest): Promise<BoardTask> {
    return apiClient.post<BoardTask>(BASE, request);
  },

  update(id: string, request: UpdateTaskRequest): Promise<BoardTask> {
    return apiClient.put<BoardTask>(`${BASE}/${id}`, request);
  },

  delete(id: string): Promise<void> {
    return apiClient.delete(`${BASE}/${id}`);
  },

  complete(id: string): Promise<void> {
    return apiClient.post(`${BASE}/${id}/complete`);
  },

  uncomplete(id: string): Promise<void> {
    return apiClient.post(`${BASE}/${id}/uncomplete`);
  },

  archive(id: string): Promise<void> {
    return apiClient.post(`${BASE}/${id}/archive`);
  },

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
