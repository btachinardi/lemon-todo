import { useRef } from 'react';
import { useInView, type Variant } from 'motion/react';

interface UseScrollAnimationOptions {
  /** Trigger once or every time element enters viewport. Default: true */
  once?: boolean;
  /** IntersectionObserver margin. Default: '-80px' */
  margin?: `${number}px` | `${number}px ${number}px`;
  /** Delay in seconds before animation starts. Default: 0 */
  delay?: number;
  /** Duration in seconds. Default: 0.6 */
  duration?: number;
}

interface ScrollAnimationResult {
  ref: React.RefObject<HTMLElement | null>;
  isInView: boolean;
  variants: { hidden: Variant; visible: Variant };
  initial: string;
  animate: string;
  transition: { duration: number; delay: number; ease: readonly number[] };
}

/** Wraps motion's useInView for consistent scroll-triggered fade-in-up animations. */
export function useScrollAnimation(options: UseScrollAnimationOptions = {}): ScrollAnimationResult {
  const { once = true, margin = '-80px' as const, delay = 0, duration = 0.6 } = options;
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once, margin });

  return {
    ref,
    isInView,
    variants: {
      hidden: { opacity: 0, y: 32 },
      visible: { opacity: 1, y: 0 },
    },
    initial: 'hidden',
    animate: isInView ? 'visible' : 'hidden',
    transition: { duration, delay, ease: [0.25, 0.46, 0.45, 0.94] },
  };
}
