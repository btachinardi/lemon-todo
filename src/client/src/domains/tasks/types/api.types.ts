import type { Priority } from './task.types';

export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  priority?: Priority;
  dueDate?: string | null;
  tags?: string[];
}

export interface UpdateTaskRequest {
  title?: string | null;
  description?: string | null;
  priority?: string | null;
  dueDate?: string | null;
  clearDueDate?: boolean;
}

export interface MoveTaskRequest {
  columnId: string;
  position: number;
}

export interface AddTagRequest {
  tag: string;
}

export interface BulkCompleteRequest {
  taskIds: string[];
}

export interface ListTasksParams {
  status?: string;
  priority?: string;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}

export interface ApiError {
  type: string;
  title: string;
  status: number;
  errors?: Record<string, string[]>;
}
