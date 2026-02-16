import { apiClient } from '@/lib/api-client';
import type { Board, Column } from '../types/board.types';

const BASE = '/api/boards';

/**
 * Client for the boards REST API (`/api/boards`).
 *
 * @throws {@link import("@/lib/api-client").ApiRequestError} on non-2xx responses:
 * - 400 validation errors (malformed request, constraint violations)
 * - 401 unauthorized (missing or invalid authentication)
 * - 404 not found (board or column does not exist)
 */
export const boardsApi = {
  /** Fetches the single default board (CP1 single-user mode). */
  getDefault(): Promise<Board> {
    return apiClient.get<Board>(`${BASE}/default`);
  },

  /** Fetches a board by ID, including its columns and card placements. */
  getById(id: string): Promise<Board> {
    return apiClient.get<Board>(`${BASE}/${id}`);
  },

  /** Appends a new column. Position defaults to end if omitted. */
  addColumn(boardId: string, name: string, position?: number): Promise<Column> {
    return apiClient.post<Column>(`${BASE}/${boardId}/columns`, { name, position });
  },

  /** Updates a column's display name. */
  renameColumn(boardId: string, columnId: string, name: string): Promise<Column> {
    return apiClient.put<Column>(`${BASE}/${boardId}/columns/${columnId}`, { name });
  },

  /** Deletes a column. Fails if the column still contains cards. */
  removeColumn(boardId: string, columnId: string): Promise<void> {
    return apiClient.deleteVoid(`${BASE}/${boardId}/columns/${columnId}`);
  },

  /** Moves a column to a new position; other columns reorder to fill the gap. */
  reorderColumn(boardId: string, columnId: string, newPosition: number): Promise<Column> {
    return apiClient.post<Column>(`${BASE}/${boardId}/columns/reorder`, { columnId, newPosition });
  },
};
