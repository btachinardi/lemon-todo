import { useState } from 'react';
import { toast } from 'sonner';
import { AlertCircleIcon } from 'lucide-react';
import { Button } from '@/ui/button';
import { Skeleton } from '@/ui/skeleton';
import { TaskListView } from '@/domains/tasks/components/views/TaskListView';
import { QuickAddForm } from '@/domains/tasks/components/widgets/QuickAddForm';
import { useTasksQuery } from '@/domains/tasks/hooks/use-tasks-query';
import { useCompleteTask, useCreateTask, useUncompleteTask } from '@/domains/tasks/hooks/use-task-mutations';
import { TaskStatus } from '@/domains/tasks/types/task.types';

export function TaskListPage() {
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

  if (tasksQuery.isLoading) {
    return (
      <div className="mx-auto max-w-4xl space-y-3 p-6">
        {[1, 2, 3, 4, 5].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    );
  }

  if (tasksQuery.error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-16">
        <AlertCircleIcon className="size-10 text-destructive/70" />
        <div className="text-center">
          <p className="font-medium">Could not load tasks</p>
          <p className="mt-1 text-sm text-muted-foreground">Check your connection and try again.</p>
        </div>
        <Button variant="outline" onClick={() => tasksQuery.refetch()}>
          Try Again
        </Button>
      </div>
    );
  }

  const tasks = tasksQuery.data?.items ?? [];

  return (
    <div className="flex flex-col">
      <div className="border-b px-4 py-3 sm:px-6">
        <div className="mx-auto max-w-4xl">
          <QuickAddForm
            onSubmit={(request) =>
              createTask.mutate(request, {
                onError: () => toast.error('Could not save task. Try again.'),
              })
            }
            isLoading={createTask.isPending}
          />
        </div>
      </div>
      <TaskListView
        tasks={tasks}
        onCompleteTask={handleToggleComplete}
        togglingTaskId={togglingTaskId}
      />
    </div>
  );
}
