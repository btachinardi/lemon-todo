import { CalendarDaysIcon, SplitIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/ui/select';
import { GroupBy } from '../../types/grouping.types';

interface ListViewToolbarProps {
  groupBy: GroupBy;
  splitCompleted: boolean;
  onGroupByChange: (groupBy: GroupBy) => void;
  onSplitCompletedChange: (split: boolean) => void;
}

const groupByOptions = [
  { value: GroupBy.None, label: 'No grouping' },
  { value: GroupBy.Day, label: 'Day' },
  { value: GroupBy.Week, label: 'Week' },
  { value: GroupBy.Month, label: 'Month' },
] as const;

export function ListViewToolbar({
  groupBy,
  splitCompleted,
  onGroupByChange,
  onSplitCompletedChange,
}: ListViewToolbarProps) {
  return (
    <div
      role="toolbar"
      aria-label="List view options"
      className="flex items-center gap-2"
    >
      <Select value={groupBy} onValueChange={(v) => onGroupByChange(v as GroupBy)}>
        <SelectTrigger size="sm" aria-label="Group by" className="gap-1.5">
          <CalendarDaysIcon className="size-3.5 text-muted-foreground" />
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {groupByOptions.map((opt) => (
            <SelectItem key={opt.value} value={opt.value}>
              {opt.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Button
        variant="outline"
        size="sm"
        aria-label="Split done"
        aria-pressed={splitCompleted}
        onClick={() => onSplitCompletedChange(!splitCompleted)}
        className={cn(
          'gap-1.5 text-xs',
          splitCompleted && 'border-lime-500/50 bg-lime-500/10 text-lime-400 hover:bg-lime-500/20 hover:text-lime-300',
        )}
      >
        <SplitIcon className="size-3.5" />
        Split done
      </Button>
    </div>
  );
}
