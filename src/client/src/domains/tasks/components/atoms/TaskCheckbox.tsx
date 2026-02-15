import { memo } from 'react';
import { LoaderIcon } from 'lucide-react';
import { cn } from '@/lib/utils';

interface TaskCheckboxProps {
  checked: boolean;
  onToggle?: () => void;
  isLoading?: boolean;
  className?: string;
}

/**
 * Animated circular checkbox with lime-green fill, checkmark stroke draw,
 * and scale bounce. Used for task completion toggling.
 */
export const TaskCheckbox = memo(function TaskCheckbox({ checked, onToggle, isLoading, className }: TaskCheckboxProps) {
  if (isLoading) {
    return (
      <div className={cn('flex size-5 shrink-0 items-center justify-center', className)}>
        <LoaderIcon className="size-4 animate-spin text-muted-foreground" />
      </div>
    );
  }

  return (
    <button
      type="button"
      onClick={(e) => {
        e.stopPropagation();
        onToggle?.();
      }}
      className={cn(
        'group relative flex size-5 shrink-0 items-center justify-center rounded-full',
        'outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-1 focus-visible:ring-offset-background',
        'transition-transform active:scale-90',
        className,
      )}
      aria-label={checked ? 'Mark as incomplete' : 'Mark as complete'}
    >
      <svg viewBox="0 0 20 20" className="size-5">
        {/* Circle */}
        <circle
          cx="10"
          cy="10"
          r="8"
          strokeWidth="1.5"
          className={cn(
            'transition-all duration-300',
            checked
              ? 'fill-primary stroke-primary'
              : 'fill-none stroke-muted-foreground/40 group-hover:stroke-primary/70',
          )}
        />
        {/* Hover glow ring (unchecked only) */}
        {!checked && (
          <circle
            cx="10"
            cy="10"
            r="8"
            strokeWidth="4"
            className="fill-none stroke-primary/0 transition-all duration-300 group-hover:stroke-primary/10"
          />
        )}
        {/* Checkmark */}
        {checked && (
          <path
            d="M6.5 10 L9 12.5 L13.5 7.5"
            fill="none"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            className="stroke-primary-foreground"
            style={{
              strokeDasharray: 16,
              strokeDashoffset: 0,
              animation: 'draw-check 0.3s ease-out, check-bounce 0.3s ease-out',
            }}
          />
        )}
      </svg>
    </button>
  );
});
