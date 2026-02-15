import { KanbanIcon } from 'lucide-react';

/** Empty state shown when the board has no tasks. */
export function EmptyBoard() {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20" data-testid="empty-board">
      <div className="rounded-full bg-secondary p-4">
        <KanbanIcon className="size-8 text-muted-foreground/50" />
      </div>
      <div className="text-center">
        <p className="text-lg font-semibold">Your board is empty</p>
        <p className="mt-1 text-sm text-muted-foreground">
          Add a task above to get started.
        </p>
      </div>
    </div>
  );
}
