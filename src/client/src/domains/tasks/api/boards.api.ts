import { apiClient } from '@/lib/api-client';
import type { Board, Column } from '../types/board.types';

const BASE = '/api/boards';

export const boardsApi = {
  getDefault(): Promise<Board> {
    return apiClient.get<Board>(`${BASE}/default`);
  },

  getById(id: string): Promise<Board> {
    return apiClient.get<Board>(`${BASE}/${id}`);
  },

  addColumn(boardId: string, name: string, position?: number): Promise<Column> {
    return apiClient.post<Column>(`${BASE}/${boardId}/columns`, { name, position });
  },

  renameColumn(boardId: string, columnId: string, name: string): Promise<Column> {
    return apiClient.put<Column>(`${BASE}/${boardId}/columns/${columnId}`, { name });
  },

  removeColumn(boardId: string, columnId: string): Promise<void> {
    return apiClient.delete(`${BASE}/${boardId}/columns/${columnId}`);
  },

  reorderColumn(boardId: string, columnId: string, newPosition: number): Promise<Column> {
    return apiClient.post<Column>(`${BASE}/${boardId}/columns/reorder`, { columnId, newPosition });
  },
};
