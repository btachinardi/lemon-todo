import { useState, useMemo, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircleIcon } from 'lucide-react';
import { toastApiError } from '@/lib/toast-helpers';
import { Button } from '@/ui/button';
import { KanbanBoard } from '@/domains/tasks/components/views/KanbanBoard';
import { BoardSkeleton } from '@/domains/tasks/components/atoms/BoardSkeleton';
import { EmptyBoard } from '@/domains/tasks/components/atoms/EmptyBoard';
import { EmptySearchResults } from '@/domains/tasks/components/atoms/EmptySearchResults';
import { QuickAddForm } from '@/domains/tasks/components/widgets/QuickAddForm';
import { TaskDetailSheetProvider } from '@/domains/tasks/components/widgets/TaskDetailSheetProvider';
import { FilterBar } from '@/domains/tasks/components/widgets/FilterBar';
import { useDefaultBoardQuery } from '@/domains/tasks/hooks/use-board-query';
import { useTasksQuery } from '@/domains/tasks/hooks/use-tasks-query';
import { useCompleteTask, useCreateTask, useMoveTask, useUncompleteTask } from '@/domains/tasks/hooks/use-task-mutations';
import type { CreateTaskRequest } from '@/domains/tasks/types/api.types';
import { useTaskViewStore } from '@/domains/tasks/stores/use-task-view-store';
import { filterTasks, hasActiveFilters } from '@/domains/tasks/utils/filter-tasks';
import { TaskStatus } from '@/domains/tasks/types/task.types';

/**
 * Kanban board page with quick-add form and toggle-complete support.
 * Supports drag-and-drop task positioning, filter bar for search/priority/status/tag, and empty state handling.
 */
export function TaskBoardPage() {
  const { t } = useTranslation();
  const boardQuery = useDefaultBoardQuery();
  const tasksQuery = useTasksQuery();
  const createTask = useCreateTask();
  const completeTask = useCompleteTask();
  const uncompleteTask = useUncompleteTask();
  const moveTask = useMoveTask();
  const [togglingTaskId, setTogglingTaskId] = useState<string | null>(null);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);

  const searchTerm = useTaskViewStore((s) => s.searchTerm);
  const filterPriority = useTaskViewStore((s) => s.filterPriority);
  const filterStatus = useTaskViewStore((s) => s.filterStatus);
  const filterTag = useTaskViewStore((s) => s.filterTag);
  const setSearchTerm = useTaskViewStore((s) => s.setSearchTerm);
  const setFilterPriority = useTaskViewStore((s) => s.setFilterPriority);
  const setFilterStatus = useTaskViewStore((s) => s.setFilterStatus);
  const setFilterTag = useTaskViewStore((s) => s.setFilterTag);
  const resetFilters = useTaskViewStore((s) => s.resetFilters);

  const allTasks = useMemo(() => tasksQuery.data?.items ?? [], [tasksQuery.data?.items]);
  const criteria = useMemo(
    () => ({ searchTerm, filterPriority, filterStatus, filterTag }),
    [searchTerm, filterPriority, filterStatus, filterTag],
  );
  const filteredTasks = useMemo(
    () => hasActiveFilters(criteria) ? filterTasks(allTasks, criteria) : allTasks,
    [allTasks, criteria],
  );

  const handleToggleComplete = useCallback(
    (id: string) => {
      const task = allTasks.find((t) => t.id === id);
      if (!task) return;

      setTogglingTaskId(id);
      const mutation = task.status === TaskStatus.Done ? uncompleteTask : completeTask;
      mutation.mutate(id, {
        onError: (error: Error) => toastApiError(error, t('tasks.errors.updateTask')),
        onSettled: () => setTogglingTaskId(null),
      });
    },
    [allTasks, completeTask, uncompleteTask, t],
  );

  const handleMoveTask = useCallback(
    (taskId: string, columnId: string, previousTaskId: string | null, nextTaskId: string | null) =>
      moveTask.mutate(
        { id: taskId, request: { columnId, previousTaskId, nextTaskId } },
        { onError: (error: Error) => toastApiError(error, t('tasks.errors.moveTask')) },
      ),
    [moveTask, t],
  );

  const handleCreateTask = useCallback(
    (request: CreateTaskRequest) =>
      createTask.mutate(request, {
        onError: (error: Error) => toastApiError(error, t('tasks.errors.saveTask')),
      }),
    [createTask, t],
  );

  const handleCloseDetail = useCallback(() => setSelectedTaskId(null), []);

  const handleClearFilters = useCallback(() => {
    resetFilters();
  }, [resetFilters]);

  if (boardQuery.isLoading || tasksQuery.isLoading) {
    return <BoardSkeleton />;
  }

  if (boardQuery.error || tasksQuery.error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-20">
        <div className="rounded-full bg-destructive/10 p-3">
          <AlertCircleIcon className="size-8 text-destructive" />
        </div>
        <div className="text-center">
          <p className="text-lg font-semibold">{t('tasks.errors.loadBoard')}</p>
          <p className="mt-1 text-base text-muted-foreground">{t('common.error.connection')}</p>
        </div>
        <Button
          variant="outline"
          onClick={() => {
            boardQuery.refetch();
            tasksQuery.refetch();
          }}
        >
          {t('common.tryAgain')}
        </Button>
      </div>
    );
  }

  const board = boardQuery.data!;
  const isFiltering = hasActiveFilters(criteria);
  const isEmpty = allTasks.length === 0;
  const isEmptyFromFilters = isFiltering && filteredTasks.length === 0;

  return (
    <div className="flex min-h-0 flex-1 flex-col pb-16 sm:pb-0">
      <div className="hidden space-y-3 border-b border-border/50 px-3 py-3 sm:block sm:px-6 sm:py-4">
        <QuickAddForm
          onSubmit={handleCreateTask}
          isLoading={createTask.isPending}
        />
      </div>
      <div className="border-b border-border/50 px-3 py-3 sm:px-6 sm:py-0 sm:pb-4">
        <FilterBar
          searchTerm={searchTerm}
          filterPriority={filterPriority}
          filterStatus={filterStatus}
          filterTag={filterTag}
          onSearchTermChange={setSearchTerm}
          onFilterPriorityChange={setFilterPriority}
          onFilterStatusChange={setFilterStatus}
          onFilterTagChange={setFilterTag}
          onResetFilters={resetFilters}
        />
      </div>
      {isEmpty ? (
        <EmptyBoard />
      ) : isEmptyFromFilters ? (
        <EmptySearchResults onClearFilters={handleClearFilters} />
      ) : (
        <KanbanBoard
          board={board}
          tasks={filteredTasks}
          onCompleteTask={handleToggleComplete}
          onSelectTask={setSelectedTaskId}
          onMoveTask={handleMoveTask}
          togglingTaskId={togglingTaskId}
          className="flex-1"
        />
      )}
      <div className="fixed inset-x-0 bottom-0 z-40 border-t border-border/50 bg-background/95 px-3 py-2 pb-[max(0.5rem,env(safe-area-inset-bottom))] backdrop-blur-xl sm:hidden">
        <QuickAddForm
          onSubmit={handleCreateTask}
          isLoading={createTask.isPending}
        />
      </div>
      <TaskDetailSheetProvider taskId={selectedTaskId} onClose={handleCloseDetail} />
    </div>
  );
}
