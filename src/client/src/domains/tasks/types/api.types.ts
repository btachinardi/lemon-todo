import type { components } from '../../../api/schema';
import type { Priority } from './task.types';

// --- Re-exported from generated OpenAPI schema ---

/** Payload for `POST /api/tasks/:id/view-note`. */
export type ViewTaskNoteRequest = components['schemas']['ViewTaskNoteRequest'];

/** Payload for `POST /api/tasks/:id/move`. Relocates a card on the board. */
export type MoveTaskRequest = components['schemas']['MoveTaskRequest'];

/** Payload for `POST /api/tasks/:id/tags`. */
export type AddTagRequest = components['schemas']['AddTagRequest'];

/** Payload for `POST /api/tasks/bulk/complete`. */
export type BulkCompleteRequest = components['schemas']['BulkCompleteRequest'];

// --- Hand-written (schema has required fields where frontend sends optional) ---

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
 * Response from `POST /api/tasks/:id/view-note`.
 * Contains the decrypted plaintext note.
 */
export interface ViewTaskNoteResponse {
  note: string;
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
