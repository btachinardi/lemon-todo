import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import type { GroupBy } from '../types/grouping.types';

type ViewMode = 'kanban' | 'list';

/**
 * Client-only UI state for the task views toolbar.
 * `viewMode`, `groupBy`, and `splitCompleted` are persisted to localStorage;
 * filters reset on page reload.
 */
interface TaskViewState {
  viewMode: ViewMode;
  groupBy: GroupBy;
  splitCompleted: boolean;
  searchTerm: string;
  filterPriority: string | undefined;
  filterStatus: string | undefined;
  setViewMode: (mode: ViewMode) => void;
  setGroupBy: (groupBy: GroupBy) => void;
  setSplitCompleted: (split: boolean) => void;
  setSearchTerm: (term: string) => void;
  setFilterPriority: (priority: string | undefined) => void;
  setFilterStatus: (status: string | undefined) => void;
  /** Clears search term and all active filters. */
  resetFilters: () => void;
}

/** Zustand store for client-only task view UI state. View mode persists to localStorage. */
export const useTaskViewStore = create<TaskViewState>()(
  persist(
    (set) => ({
      viewMode: 'kanban',
      groupBy: 'none' as GroupBy,
      splitCompleted: false,
      searchTerm: '',
      filterPriority: undefined,
      filterStatus: undefined,
      setViewMode: (viewMode) => set({ viewMode }),
      setGroupBy: (groupBy) => set({ groupBy }),
      setSplitCompleted: (splitCompleted) => set({ splitCompleted }),
      setSearchTerm: (searchTerm) => set({ searchTerm }),
      setFilterPriority: (filterPriority) => set({ filterPriority }),
      setFilterStatus: (filterStatus) => set({ filterStatus }),
      resetFilters: () =>
        set({ searchTerm: '', filterPriority: undefined, filterStatus: undefined }),
    }),
    {
      name: 'lemondo-task-view',
      partialize: (state) => ({
        viewMode: state.viewMode,
        groupBy: state.groupBy,
        splitCompleted: state.splitCompleted,
      }),
    },
  ),
);
