import { toast } from 'sonner';
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

  const handleToggleComplete = (id: string) => {
    const task = tasksQuery.data?.items.find((t) => t.id === id);
    if (!task) return;

    if (task.status === TaskStatus.Done) {
      uncompleteTask.mutate(id, {
        onError: () => toast.error('Failed to uncomplete task'),
      });
    } else {
      completeTask.mutate(id, {
        onError: () => toast.error('Failed to complete task'),
      });
    }
  };

  if (tasksQuery.isLoading) {
    return (
      <div className="space-y-3 p-6">
        {[1, 2, 3, 4, 5].map((i) => (
          <Skeleton key={i} className="h-16 w-full" />
        ))}
      </div>
    );
  }

  if (tasksQuery.error) {
    return (
      <div className="flex flex-col items-center justify-center py-16">
        <p className="text-destructive">Failed to load tasks</p>
      </div>
    );
  }

  const tasks = tasksQuery.data?.items ?? [];

  return (
    <div className="flex flex-col">
      <div className="border-b px-6 py-3">
        <QuickAddForm
          onSubmit={(request) =>
            createTask.mutate(request, {
              onError: () => toast.error('Failed to create task'),
            })
          }
          isLoading={createTask.isPending}
        />
      </div>
      <TaskListView tasks={tasks} onCompleteTask={handleToggleComplete} />
    </div>
  );
}
