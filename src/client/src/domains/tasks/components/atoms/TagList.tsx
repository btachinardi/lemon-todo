import { memo } from 'react';
import { Badge } from '@/ui/badge';
import { cn } from '@/lib/utils';

interface TagListProps {
  tags: string[];
  className?: string;
  /** When provided, each tag shows an "x" button that calls back with the tag value. */
  onRemove?: (tag: string) => void;
}

/** Horizontal list of tag badges. Renders nothing when `tags` is empty. */
export const TagList = memo(function TagList({ tags, className, onRemove }: TagListProps) {
  if (tags.length === 0) return null;

  return (
    <div className={cn('flex flex-wrap gap-1', className)}>
      {tags.map((tag) => (
        <Badge key={tag} variant="secondary" className="text-xs">
          {tag}
          {onRemove && (
            <button
              type="button"
              onClick={() => onRemove(tag)}
              className="ml-1 hover:text-destructive"
              aria-label={`Remove tag ${tag}`}
            >
              &times;
            </button>
          )}
        </Badge>
      ))}
    </div>
  );
});
