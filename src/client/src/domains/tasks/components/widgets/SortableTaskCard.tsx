import { memo } from 'react';
import { useSortable } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import type { Task } from '../../types/task.types';
import { TaskCard } from './TaskCard';

interface SortableTaskCardProps {
  task: Task;
  onComplete?: (id: string) => void;
  onSelect?: (id: string) => void;
  isToggling?: boolean;
}

/**
 * Wraps {@link TaskCard} with @dnd-kit sortable behavior.
 * Provides drag listeners, transform, and dragging state.
 *
 * Wrapped in `React.memo` — rendered inside `.map()` in KanbanColumn.
 */
export const SortableTaskCard = memo(function SortableTaskCard({ task, onComplete, onSelect, isToggling }: SortableTaskCardProps) {
  const {
    attributes,
    listeners,
    setNodeRef,
    transform,
    transition,
    isDragging,
  } = useSortable({ id: task.id });

  const style = {
    transform: CSS.Transform.toString(transform),
    transition,
  };

  return (
    <div ref={setNodeRef} style={style} className="animate-fade-in" {...attributes} {...listeners}>
      <TaskCard
        task={task}
        onComplete={onComplete}
        onSelect={onSelect}
        isToggling={isToggling}
        isDragging={isDragging}
      />
    </div>
  );
});
