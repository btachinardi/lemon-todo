import { toast } from 'sonner';
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
      <div className="flex flex-col items-center justify-center py-16">
        <p className="text-destructive">Failed to load board</p>
      </div>
    );
  }

  const board = boardQuery.data!;
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
      <KanbanBoard
        board={board}
        tasks={tasks}
        onCompleteTask={handleToggleComplete}
        className="flex-1"
      />
    </div>
  );
}
