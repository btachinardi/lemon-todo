import { useState } from 'react';
import { AlertCircleIcon } from 'lucide-react';
import { toastApiError } from '@/lib/toast-helpers';
import { Button } from '@/ui/button';
import { Skeleton } from '@/ui/skeleton';
import { KanbanBoard } from '@/domains/tasks/components/views/KanbanBoard';
import { QuickAddForm } from '@/domains/tasks/components/widgets/QuickAddForm';
import { useDefaultBoardQuery } from '@/domains/tasks/hooks/use-board-query';
import { useTasksQuery } from '@/domains/tasks/hooks/use-tasks-query';
import { useCompleteTask, useCreateTask, useMoveTask, useUncompleteTask } from '@/domains/tasks/hooks/use-task-mutations';
import { TaskStatus } from '@/domains/tasks/types/task.types';

/** Kanban board page with quick-add form and toggle-complete support. */
export function TaskBoardPage() {
  const boardQuery = useDefaultBoardQuery();
  const tasksQuery = useTasksQuery();
  const createTask = useCreateTask();
  const completeTask = useCompleteTask();
  const uncompleteTask = useUncompleteTask();
  const moveTask = useMoveTask();
  const [togglingTaskId, setTogglingTaskId] = useState<string | null>(null);

  const handleToggleComplete = (id: string) => {
    const task = tasksQuery.data?.items.find((t) => t.id === id);
    if (!task) return;

    setTogglingTaskId(id);
    const mutation = task.status === TaskStatus.Done ? uncompleteTask : completeTask;
    mutation.mutate(id, {
      onError: (error: Error) => toastApiError(error, 'Could not update task. Try again.'),
      onSettled: () => setTogglingTaskId(null),
    });
  };

  if (boardQuery.isLoading || tasksQuery.isLoading) {
    return (
      <div className="flex gap-4 p-6">
        {[1, 2, 3].map((i) => (
          <div key={i} className="min-w-72 flex-1 space-y-3 rounded-xl bg-secondary/40 p-3">
            <Skeleton className="h-5 w-24" />
            <Skeleton className="h-20 w-full rounded-lg" />
            <Skeleton className="h-20 w-full rounded-lg" />
          </div>
        ))}
      </div>
    );
  }

  if (boardQuery.error || tasksQuery.error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-20">
        <div className="rounded-full bg-destructive/10 p-3">
          <AlertCircleIcon className="size-8 text-destructive" />
        </div>
        <div className="text-center">
          <p className="text-lg font-semibold">Could not load your board</p>
          <p className="mt-1 text-sm text-muted-foreground">Check your connection and try again.</p>
        </div>
        <Button
          variant="outline"
          className=""
          onClick={() => {
            boardQuery.refetch();
            tasksQuery.refetch();
          }}
        >
          Try Again
        </Button>
      </div>
    );
  }

  const board = boardQuery.data!;
  const tasks = tasksQuery.data?.items ?? [];

  return (
    <div className="flex flex-col">
      <div className="border-b border-border/50 px-6 py-4">
        <QuickAddForm
          onSubmit={(request) =>
            createTask.mutate(request, {
              onError: (error: Error) => toastApiError(error, 'Could not save task. Try again.'),
            })
          }
          isLoading={createTask.isPending}
        />
      </div>
      <KanbanBoard
        board={board}
        tasks={tasks}
        onCompleteTask={handleToggleComplete}
        onMoveTask={(taskId, columnId, previousTaskId, nextTaskId) =>
          moveTask.mutate(
            { id: taskId, request: { columnId, previousTaskId, nextTaskId } },
            { onError: (error: Error) => toastApiError(error, 'Could not move task. Try again.') },
          )
        }
        togglingTaskId={togglingTaskId}
        className="flex-1"
      />
    </div>
  );
}
