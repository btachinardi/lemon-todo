import { Skeleton } from '@/ui/skeleton';

/** 3-column skeleton placeholder shown while the kanban board is loading. */
export function BoardSkeleton() {
  return (
    <div className="flex gap-4 p-6" data-testid="board-skeleton">
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
