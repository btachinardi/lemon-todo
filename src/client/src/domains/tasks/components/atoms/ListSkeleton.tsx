import { Skeleton } from '@/ui/skeleton';

/** Row skeleton placeholder shown while the task list is loading. */
export function ListSkeleton() {
  return (
    <div className="mx-auto max-w-4xl space-y-3 p-6" data-testid="list-skeleton">
      {[1, 2, 3, 4, 5].map((i) => (
        <Skeleton key={i} className="h-14 w-full rounded-lg" />
      ))}
    </div>
  );
}
