import { useEffect, useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { SearchIcon, XIcon } from 'lucide-react';
import { Input } from '@/ui/input';
import { Button } from '@/ui/button';
import { Badge } from '@/ui/badge';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/ui/select';
import { Priority, TaskStatus } from '../../types/task.types';

interface FilterBarProps {
  searchTerm: string;
  filterPriority: string | undefined;
  filterStatus: string | undefined;
  filterTag: string | undefined;
  onSearchTermChange: (term: string) => void;
  onFilterPriorityChange: (priority: string | undefined) => void;
  onFilterStatusChange: (status: string | undefined) => void;
  onFilterTagChange: (tag: string | undefined) => void;
  onResetFilters: () => void;
}

/** Debounced search + dropdown filters for tasks. */
export function FilterBar({
  searchTerm,
  filterPriority,
  filterStatus,
  filterTag,
  onSearchTermChange,
  onFilterPriorityChange,
  onFilterStatusChange,
  onFilterTagChange,
  onResetFilters,
}: FilterBarProps) {
  const { t } = useTranslation();
  const [localSearch, setLocalSearch] = useState(searchTerm);

  // Debounce search input (300ms)
  useEffect(() => {
    const timer = setTimeout(() => onSearchTermChange(localSearch), 300);
    return () => clearTimeout(timer);
  }, [localSearch, onSearchTermChange]);

  // Sync external resets back to local
  useEffect(() => {
    setLocalSearch(searchTerm);
  }, [searchTerm]);

  const activeFilterCount =
    (searchTerm ? 1 : 0) +
    (filterPriority ? 1 : 0) +
    (filterStatus ? 1 : 0) +
    (filterTag ? 1 : 0);

  const handleReset = useCallback(() => {
    onResetFilters();
    setLocalSearch('');
  }, [onResetFilters]);

  return (
    <div className="flex flex-wrap items-center gap-2">
      <div className="relative min-w-0 flex-1 basis-full sm:basis-auto sm:min-w-[200px]">
        <SearchIcon className="absolute left-2.5 top-1/2 -translate-y-1/2 size-4 text-muted-foreground" />
        <Input
          value={localSearch}
          onChange={(e) => setLocalSearch(e.target.value)}
          placeholder={t('tasks.filter.searchPlaceholder')}
          className="h-9 pl-9"
          aria-label="Search tasks"
        />
        {localSearch && (
          <button
            className="absolute right-2 top-1/2 -translate-y-1/2 rounded-full p-0.5 hover:bg-secondary"
            onClick={() => {
              setLocalSearch('');
              onSearchTermChange('');
            }}
            aria-label={t('tasks.filter.clearSearch')}
          >
            <XIcon className="size-3.5" />
          </button>
        )}
      </div>

      <Select
        value={filterPriority ?? '__all__'}
        onValueChange={(v) => onFilterPriorityChange(v === '__all__' ? undefined : v)}
      >
        <SelectTrigger className="h-9 w-auto min-w-[110px] sm:w-[130px] text-sm" aria-label="Filter by priority">
          <SelectValue placeholder="Priority" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="__all__">{t('tasks.filter.allPriorities')}</SelectItem>
          {Object.values(Priority)
            .filter((p) => p !== 'None')
            .map((p) => (
              <SelectItem key={p} value={p}>
                {t(`tasks.priority.${p.toLowerCase()}`)}
              </SelectItem>
            ))}
        </SelectContent>
      </Select>

      <Select
        value={filterStatus ?? '__all__'}
        onValueChange={(v) => onFilterStatusChange(v === '__all__' ? undefined : v)}
      >
        <SelectTrigger className="h-9 w-auto min-w-[110px] sm:w-[130px] text-sm" aria-label="Filter by status">
          <SelectValue placeholder="Status" />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="__all__">{t('tasks.filter.allStatuses')}</SelectItem>
          {Object.values(TaskStatus).map((s) => (
            <SelectItem key={s} value={s}>
              {t(`tasks.status.${s === 'InProgress' ? 'inProgress' : s.toLowerCase()}`)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {filterTag && (
        <Badge variant="secondary" className="gap-1 pr-1">
          tag: {filterTag}
          <button
            onClick={() => onFilterTagChange(undefined)}
            className="ml-0.5 rounded-full p-0.5 hover:bg-destructive/20 transition-colors"
            aria-label={`Remove tag filter ${filterTag}`}
          >
            <XIcon className="size-3" />
          </button>
        </Badge>
      )}

      {activeFilterCount > 0 && (
        <Button
          variant="ghost"
          size="sm"
          className="h-9 gap-1.5 text-sm"
          onClick={handleReset}
        >
          <XIcon className="size-3.5" />
          {t('common.clear')}
          <Badge variant="secondary" className="ml-1 px-1.5 py-0 text-[10px]">
            {activeFilterCount}
          </Badge>
        </Button>
      )}
    </div>
  );
}
