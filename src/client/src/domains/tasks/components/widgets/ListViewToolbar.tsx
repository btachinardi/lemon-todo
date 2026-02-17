import { CalendarDaysIcon, SplitIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';
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
  { value: GroupBy.None, key: 'tasks.listToolbar.noGrouping' },
  { value: GroupBy.Day, key: 'tasks.listToolbar.day' },
  { value: GroupBy.Week, key: 'tasks.listToolbar.week' },
  { value: GroupBy.Month, key: 'tasks.listToolbar.month' },
] as const;

export function ListViewToolbar({
  groupBy,
  splitCompleted,
  onGroupByChange,
  onSplitCompletedChange,
}: ListViewToolbarProps) {
  const { t } = useTranslation();
  return (
    <div
      role="toolbar"
      aria-label={t('tasks.listToolbar.label')}
      className="flex items-center gap-2"
    >
      <Select value={groupBy} onValueChange={(v) => onGroupByChange(v as GroupBy)}>
        <SelectTrigger size="sm" aria-label={t('tasks.listToolbar.groupBy')} className="gap-1.5">
          <CalendarDaysIcon className="size-3.5 text-muted-foreground" />
          <SelectValue />
        </SelectTrigger>
        <SelectContent>
          {groupByOptions.map((opt) => (
            <SelectItem key={opt.value} value={opt.value}>
              {t(opt.key)}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      <Button
        variant="outline"
        size="sm"
        aria-label={t('tasks.listToolbar.splitDone')}
        aria-pressed={splitCompleted}
        onClick={() => onSplitCompletedChange(!splitCompleted)}
        className={cn(
          'gap-1.5 text-sm',
          splitCompleted && 'border-lime-500/50 bg-lime-500/10 text-lime-400 hover:bg-lime-500/20 hover:text-lime-300',
        )}
      >
        <SplitIcon className="size-3.5" />
        <span className="hidden sm:inline">{t('tasks.listToolbar.splitDone')}</span>
      </Button>
    </div>
  );
}
