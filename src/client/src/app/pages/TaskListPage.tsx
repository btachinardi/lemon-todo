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

/** Flat list page with quick-add form and toggle-complete support. */
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
          <Skeleton key={i} className="h-14 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  if (tasksQuery.error) {
    return (
      <div className="flex flex-col items-center justify-center gap-4 py-20">
        <div className="rounded-full bg-destructive/10 p-3">
          <AlertCircleIcon className="size-8 text-destructive" />
        </div>
        <div className="text-center">
          <p className="font-display font-semibold">Could not load tasks</p>
          <p className="mt-1 text-sm text-muted-foreground">Check your connection and try again.</p>
        </div>
        <Button variant="outline" className="rounded-full" onClick={() => tasksQuery.refetch()}>
          Try Again
        </Button>
      </div>
    );
  }

  const tasks = tasksQuery.data?.items ?? [];

  return (
    <div className="flex flex-col">
      <div className="border-b border-border/50 px-6 py-4">
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
