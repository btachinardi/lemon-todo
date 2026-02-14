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
import type { TaskItem } from '../types/task.types';

const BASE = '/api/tasks';

export const tasksApi = {
  list(params?: ListTasksParams): Promise<PagedResult<TaskItem>> {
    return apiClient.get<PagedResult<TaskItem>>(BASE, params as Record<string, string | number | undefined>);
  },

  getById(id: string): Promise<TaskItem> {
    return apiClient.get<TaskItem>(`${BASE}/${id}`);
  },

  create(request: CreateTaskRequest): Promise<TaskItem> {
    return apiClient.post<TaskItem>(BASE, request);
  },

  update(id: string, request: UpdateTaskRequest): Promise<TaskItem> {
    return apiClient.put<TaskItem>(`${BASE}/${id}`, request);
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
