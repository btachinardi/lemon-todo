import { useMemo, useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { AlertCircleIcon } from 'lucide-react';
import { toastApiError } from '@/lib/toast-helpers';
import { Button } from '@/ui/button';
import { TaskListView } from '@/domains/tasks/components/views/TaskListView';
import { ListSkeleton } from '@/domains/tasks/components/atoms/ListSkeleton';
import { ListViewToolbar } from '@/domains/tasks/components/widgets/ListViewToolbar';
import { QuickAddForm } from '@/domains/tasks/components/widgets/QuickAddForm';
import { TaskDetailSheetProvider } from '@/domains/tasks/components/widgets/TaskDetailSheetProvider';
import { useTasksQuery } from '@/domains/tasks/hooks/use-tasks-query';
import { useCompleteTask, useCreateTask, useUncompleteTask } from '@/domains/tasks/hooks/use-task-mutations';
import type { CreateTaskRequest } from '@/domains/tasks/types/api.types';
import { useTaskViewStore } from '@/domains/tasks/stores/use-task-view-store';
import { TaskStatus } from '@/domains/tasks/types/task.types';
import { GroupBy } from '@/domains/tasks/types/grouping.types';
import { groupTasksByDate } from '@/domains/tasks/utils/group-tasks';

/**
 * List page with grouping toolbar, quick-add form, and toggle-complete support.
 * Supports grouping by due date, priority, or status, with optional split view for completed tasks.
 */
export function TaskListPage() {
  const { t } = useTranslation();
  const tasksQuery = useTasksQuery();
  const createTask = useCreateTask();
  const completeTask = useCompleteTask();
  const uncompleteTask = useUncompleteTask();
  const [togglingTaskId, setTogglingTaskId] = useState<string | null>(null);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);

  const groupBy = useTaskViewStore((s) => s.groupBy);
  const splitCompleted = useTaskViewStore((s) => s.splitCompleted);
  const setGroupBy = useTaskViewStore((s) => s.setGroupBy);
  const setSplitCompleted = useTaskViewStore((s) => s.setSplitCompleted);

  const tasks = useMemo(() => tasksQuery.data?.items ?? [], [tasksQuery.data?.items]);
  const groups = useMemo(
    () => groupTasksByDate(tasks, groupBy, splitCompleted),
    [tasks, groupBy, splitCompleted],
  );

  const handleToggleComplete = useCallback(
    (id: string) => {
      const task = tasks.find((t) => t.id === id);
      if (!task) return;

      setTogglingTaskId(id);
      const mutation = task.status === TaskStatus.Done ? uncompleteTask : completeTask;
      mutation.mutate(id, {
        onError: (error: Error) => toastApiError(error, t('tasks.errors.updateTask')),
        onSettled: () => setTogglingTaskId(null),
      });
    },
    [tasks, completeTask, uncompleteTask, t],
  );

  const handleCreateTask = useCallback(
    (request: CreateTaskRequest) =>
      createTask.mutate(request, {
        onError: (error: Error) => toastApiError(error, t('tasks.errors.saveTask')),
      }),
    [createTask, t],
  );

  const handleCloseDetail = useCallback(() => setSelectedTaskId(null), []);

  if (tasksQuery.isLoading) {
    return <ListSkeleton />;
  }

  if (tasksQuery.error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-20">
        <div className="rounded-full bg-destructive/10 p-3">
          <AlertCircleIcon className="size-8 text-destructive" />
        </div>
        <div className="text-center">
          <p className="text-lg font-semibold">{t('tasks.errors.loadTasks')}</p>
          <p className="mt-1 text-sm text-muted-foreground">{t('common.error.connection')}</p>
        </div>
        <Button variant="outline" onClick={() => tasksQuery.refetch()}>
          {t('common.tryAgain')}
        </Button>
      </div>
    );
  }

  return (
    <div className="flex flex-col pb-16 sm:pb-0">
      <div className="border-b border-border/50 px-3 py-3 sm:px-6 sm:py-4">
        <div className="mx-auto flex max-w-4xl items-center gap-4">
          <div className="hidden flex-1 sm:block">
            <QuickAddForm
              onSubmit={handleCreateTask}
              isLoading={createTask.isPending}
            />
          </div>
          <ListViewToolbar
            groupBy={groupBy}
            splitCompleted={splitCompleted}
            onGroupByChange={setGroupBy}
            onSplitCompletedChange={setSplitCompleted}
          />
        </div>
      </div>
      <TaskListView
        groups={groups}
        showGroupHeaders={groupBy !== GroupBy.None}
        onCompleteTask={handleToggleComplete}
        onSelectTask={setSelectedTaskId}
        togglingTaskId={togglingTaskId}
      />
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
