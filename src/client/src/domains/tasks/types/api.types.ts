import type { Priority } from './task.types';

/**
 * Payload for `POST /api/tasks`. Creates a new task and places it on the default board.
 * Only `title` is required by the backend.
 */
export interface CreateTaskRequest {
  title: string;
  description?: string | null;
  priority?: Priority;
  /** ISO 8601 date string (date only). */
  dueDate?: string | null;
  tags?: string[];
  /** Plaintext sensitive note — encrypted at rest by the backend. */
  sensitiveNote?: string | null;
}

/**
 * Payload for `PUT /api/tasks/:id`. All fields are optional --
 * only provided fields are updated (patch semantics).
 */
export interface UpdateTaskRequest {
  title?: string | null;
  description?: string | null;
  priority?: string | null;
  /** ISO 8601 date string. Ignored when `clearDueDate` is true. */
  dueDate?: string | null;
  /** Explicitly remove the due date (takes precedence over `dueDate`). */
  clearDueDate?: boolean;
  /** Plaintext sensitive note — encrypted at rest by the backend. */
  sensitiveNote?: string | null;
  /** Explicitly remove the sensitive note. */
  clearSensitiveNote?: boolean;
}

/**
 * Payload for `POST /api/tasks/:id/view-note`.
 * Re-authenticates the user before decrypting the sensitive note.
 */
export interface ViewTaskNoteRequest {
  password: string;
}

/**
 * Response from `POST /api/tasks/:id/view-note`.
 * Contains the decrypted plaintext note.
 */
export interface ViewTaskNoteResponse {
  note: string;
}

/** Payload for `POST /api/tasks/:id/move`. Relocates a card on the board. */
export interface MoveTaskRequest {
  columnId: string;
  /** ID of the card directly above the drop target, or `null` at the top. */
  previousTaskId: string | null;
  /** ID of the card directly below the drop target, or `null` at the bottom. */
  nextTaskId: string | null;
}

/**
 * Payload for `POST /api/tasks/:id/tags`.
 * Appends a tag to the task. Silently succeeds if the tag already exists.
 */
export interface AddTagRequest {
  tag: string;
}

/**
 * Payload for `POST /api/tasks/bulk/complete`.
 * Marks multiple tasks as complete in a single request.
 */
export interface BulkCompleteRequest {
  taskIds: string[];
}

/** Query parameters for `GET /api/tasks`. All filters are optional. */
export interface ListTasksParams {
  status?: string;
  priority?: string;
  /** Case-insensitive substring match on task title and description. */
  search?: string;
  /** Exact match on a tag value. */
  tag?: string;
  /** One-based page number. @defaultValue 1 */
  page?: number;
  /** @defaultValue 50 */
  pageSize?: number;
}

/** Envelope for paginated list endpoints. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  /** Current one-based page number. */
  page: number;
  pageSize: number;
}

/**
 * RFC 7807 problem details returned by the API on validation or
 * business rule failures.
 */
export interface ApiError {
  /** Machine-readable error category URI. */
  type: string;
  /** Human-readable summary of the problem. */
  title: string;
  status: number;
  /** Field-level validation errors keyed by property name. */
  errors?: Record<string, string[]>;
}
