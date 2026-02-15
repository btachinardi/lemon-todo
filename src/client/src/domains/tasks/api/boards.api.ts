import { apiClient } from '@/lib/api-client';
import type { Board, Column } from '../types/board.types';

const BASE = '/api/boards';

/**
 * Client for the boards REST API (`/api/boards`).
 *
 * @throws {@link import("@/lib/api-client").ApiRequestError} on non-2xx responses.
 */
export const boardsApi = {
  /** Fetches the single default board (CP1 single-user mode). */
  getDefault(): Promise<Board> {
    return apiClient.get<Board>(`${BASE}/default`);
  },

  getById(id: string): Promise<Board> {
    return apiClient.get<Board>(`${BASE}/${id}`);
  },

  /** Appends a new column. Position defaults to end if omitted. */
  addColumn(boardId: string, name: string, position?: number): Promise<Column> {
    return apiClient.post<Column>(`${BASE}/${boardId}/columns`, { name, position });
  },

  renameColumn(boardId: string, columnId: string, name: string): Promise<Column> {
    return apiClient.put<Column>(`${BASE}/${boardId}/columns/${columnId}`, { name });
  },

  removeColumn(boardId: string, columnId: string): Promise<void> {
    return apiClient.delete(`${BASE}/${boardId}/columns/${columnId}`);
  },

  /** Moves a column to a new position; other columns reorder to fill the gap. */
  reorderColumn(boardId: string, columnId: string, newPosition: number): Promise<Column> {
    return apiClient.post<Column>(`${BASE}/${boardId}/columns/reorder`, { columnId, newPosition });
  },
};
