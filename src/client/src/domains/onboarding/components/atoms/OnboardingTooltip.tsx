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
  const [coords, setCoords] = useState<{ top: number; left: number; arrowLeft: number } | null>(
    null,
  );

  useEffect(() => {
    if (!visible) return;

    const EDGE_PADDING = 12;

    function updatePosition() {
      const target = document.querySelector(targetSelector);
      if (!target) return;

      const rect = target.getBoundingClientRect();
      const tooltipWidth = tooltipRef.current?.offsetWidth ?? 0;
      const tooltipHeight = tooltipRef.current?.offsetHeight ?? 0;
      const viewportWidth = window.innerWidth;

      const top = position === 'bottom' ? rect.bottom + EDGE_PADDING : rect.top - tooltipHeight - EDGE_PADDING;

      // Center on target, then clamp to stay within viewport
      const targetCenter = rect.left + rect.width / 2;
      let left = targetCenter - tooltipWidth / 2;
      left = Math.max(EDGE_PADDING, Math.min(left, viewportWidth - tooltipWidth - EDGE_PADDING));

      // Arrow offset: pixel distance from tooltip's left edge to target center
      const arrowLeft = Math.max(16, Math.min(targetCenter - left, tooltipWidth - 16));

      setCoords({ top, left, arrowLeft });
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
        'fixed z-[60] max-w-[min(20rem,calc(100vw-1.5rem))] rounded-lg border bg-card p-4 shadow-xl',
        'animate-in fade-in-0 zoom-in-95',
      )}
      style={{ top: coords.top, left: coords.left }}
      role="tooltip"
    >
      <div
        className={cn(
          'absolute -translate-x-1/2 border-[6px] border-transparent',
          position === 'bottom'
            ? '-top-3 border-b-border'
            : '-bottom-3 border-t-border',
        )}
        style={{ left: coords.arrowLeft }}
      />
      {children}
    </div>
  );
}
