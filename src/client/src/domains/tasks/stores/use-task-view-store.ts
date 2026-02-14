import { create } from 'zustand';
import { persist } from 'zustand/middleware';

type ViewMode = 'kanban' | 'list';

interface TaskViewState {
  viewMode: ViewMode;
  searchTerm: string;
  filterPriority: string | undefined;
  filterStatus: string | undefined;
  setViewMode: (mode: ViewMode) => void;
  setSearchTerm: (term: string) => void;
  setFilterPriority: (priority: string | undefined) => void;
  setFilterStatus: (status: string | undefined) => void;
  resetFilters: () => void;
}

export const useTaskViewStore = create<TaskViewState>()(
  persist(
    (set) => ({
      viewMode: 'kanban',
      searchTerm: '',
      filterPriority: undefined,
      filterStatus: undefined,
      setViewMode: (viewMode) => set({ viewMode }),
      setSearchTerm: (searchTerm) => set({ searchTerm }),
      setFilterPriority: (filterPriority) => set({ filterPriority }),
      setFilterStatus: (filterStatus) => set({ filterStatus }),
      resetFilters: () =>
        set({ searchTerm: '', filterPriority: undefined, filterStatus: undefined }),
    }),
    {
      name: 'lemondo-task-view',
      partialize: (state) => ({ viewMode: state.viewMode }),
    },
  ),
);
