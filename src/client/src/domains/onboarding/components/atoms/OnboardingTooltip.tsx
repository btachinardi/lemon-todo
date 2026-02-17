import { useEffect, useRef, useState, type ReactNode } from 'react';
import { cn } from '@/lib/utils';

interface OnboardingTooltipProps {
  targetSelector: string;
  children: ReactNode;
  position?: 'top' | 'bottom';
  visible: boolean;
}

/**
 * Positioned tooltip that attaches itself near a target element.
 * Uses getBoundingClientRect for positioning with a fixed overlay.
 */
export function OnboardingTooltip({
  targetSelector,
  children,
  position = 'bottom',
  visible,
}: OnboardingTooltipProps) {
  const tooltipRef = useRef<HTMLDivElement>(null);
  const [coords, setCoords] = useState<{ top: number; left: number } | null>(null);

  useEffect(() => {
    if (!visible) return;

    function updatePosition() {
      const target = document.querySelector(targetSelector);
      if (!target) return;

      const rect = target.getBoundingClientRect();
      const tooltipHeight = tooltipRef.current?.offsetHeight ?? 0;

      setCoords({
        top: position === 'bottom' ? rect.bottom + 12 : rect.top - tooltipHeight - 12,
        left: rect.left + rect.width / 2,
      });
    }

    updatePosition();
    window.addEventListener('resize', updatePosition);
    const observer = new MutationObserver(updatePosition);
    observer.observe(document.body, { childList: true, subtree: true });

    return () => {
      window.removeEventListener('resize', updatePosition);
      observer.disconnect();
    };
  }, [targetSelector, position, visible]);

  if (!visible || !coords) return null;

  return (
    <div
      ref={tooltipRef}
      className={cn(
        'fixed z-[60] max-w-xs -translate-x-1/2 rounded-lg border bg-card p-4 shadow-xl',
        'animate-in fade-in-0 zoom-in-95',
      )}
      style={{ top: coords.top, left: coords.left }}
      role="tooltip"
    >
      <div
        className={cn(
          'absolute left-1/2 -translate-x-1/2 border-[6px] border-transparent',
          position === 'bottom'
            ? '-top-3 border-b-border'
            : '-bottom-3 border-t-border',
        )}
      />
      {children}
    </div>
  );
}
