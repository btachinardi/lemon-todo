import { useState } from 'react';
import { toast } from 'sonner';
import { AlertCircleIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import { Skeleton } from '@/ui/skeleton';
import { KanbanBoard } from '@/domains/tasks/components/views/KanbanBoard';
import { QuickAddForm } from '@/domains/tasks/components/widgets/QuickAddForm';
import { useDefaultBoardQuery } from '@/domains/tasks/hooks/use-board-query';
import { useTasksQuery } from '@/domains/tasks/hooks/use-tasks-query';
import { useCompleteTask, useCreateTask, useUncompleteTask } from '@/domains/tasks/hooks/use-task-mutations';
import { TaskStatus } from '@/domains/tasks/types/task.types';

export function TaskBoardPage() {
  const boardQuery = useDefaultBoardQuery();
  const tasksQuery = useTasksQuery();
  const createTask = useCreateTask();
  const completeTask = useCompleteTask();
  const uncompleteTask = useUncompleteTask();
  const [togglingTaskId, setTogglingTaskId] = useState<string | null>(null);

  const handleToggleComplete = (id: string) => {
    const task = tasksQuery.data?.items.find((t) => t.id === id);
    if (!task) return;

    setTogglingTaskId(id);
    const mutation = task.status === TaskStatus.Done ? uncompleteTask : completeTask;
    mutation.mutate(id, {
      onError: () => toast.error('Could not update task. Try again.'),
      onSettled: () => setTogglingTaskId(null),
    });
  };

  if (boardQuery.isLoading || tasksQuery.isLoading) {
    return (
      <div className="flex gap-4 p-4">
        {[1, 2, 3].map((i) => (
          <div key={i} className="w-72 shrink-0 space-y-3 rounded-lg bg-muted/50 p-3">
            <Skeleton className="h-6 w-24" />
            <Skeleton className="h-24 w-full" />
            <Skeleton className="h-24 w-full" />
          </div>
        ))}
      </div>
    );
  }

  if (boardQuery.error || tasksQuery.error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-16">
        <AlertCircleIcon className="size-10 text-destructive/70" />
        <div className="text-center">
          <p className="font-medium">Could not load your board</p>
          <p className="mt-1 text-sm text-muted-foreground">Check your connection and try again.</p>
        </div>
        <Button
          variant="outline"
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
      <div className="border-b px-4 py-3 sm:px-6">
        <QuickAddForm
          onSubmit={(request) =>
            createTask.mutate(request, {
              onError: () => toast.error('Could not save task. Try again.'),
            })
          }
          isLoading={createTask.isPending}
        />
      </div>
      <KanbanBoard
        board={board}
        tasks={tasks}
        onCompleteTask={handleToggleComplete}
        togglingTaskId={togglingTaskId}
        className="flex-1"
      />
    </div>
  );
}
