import { useTranslation } from 'react-i18next';
import { KanbanIcon } from 'lucide-react';

/** Empty state shown when the board has no tasks. */
export function EmptyBoard() {
  const { t } = useTranslation();

  return (
    <div className="flex flex-col items-center justify-center gap-3 py-20" data-testid="empty-board">
      <div className="rounded-full bg-secondary p-4">
        <KanbanIcon className="size-8 text-muted-foreground/50" />
      </div>
      <div className="text-center">
        <p className="text-lg font-semibold">{t('tasks.empty.boardTitle')}</p>
        <p className="mt-1 text-sm text-muted-foreground">
          {t('tasks.empty.boardSubtitle')}
        </p>
      </div>
    </div>
  );
}
