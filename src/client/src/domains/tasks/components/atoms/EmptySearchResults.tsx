import { SearchIcon } from 'lucide-react';
import { Button } from '@/ui/button';

interface EmptySearchResultsProps {
  onClearFilters: () => void;
}

/** Empty state shown when active filters produce no matching tasks. */
export function EmptySearchResults({ onClearFilters }: EmptySearchResultsProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20" data-testid="empty-search-results">
      <div className="rounded-full bg-secondary p-4">
        <SearchIcon className="size-8 text-muted-foreground/50" />
      </div>
      <div className="text-center">
        <p className="text-lg font-semibold">No matching tasks</p>
        <p className="mt-1 text-sm text-muted-foreground">
          Try adjusting your filters or search term.
        </p>
      </div>
      <Button variant="outline" size="sm" onClick={onClearFilters}>
        Clear filters
      </Button>
    </div>
  );
}
