import { useEffect, useRef, useState } from 'react';
import { motion, type Variant } from 'motion/react';
import { cn } from '@/lib/utils';

interface MetricCardProps {
  value: string;
  label: string;
  detail: string;
  index: number;
  isInView: boolean;
  className?: string;
}

const hidden: Variant = { opacity: 0, y: 24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.6, delay: i * 0.1, ease: [0.25, 0.46, 0.45, 0.94] },
});

/** Parses the numeric portion of a metric value string (e.g. "700+" -> 700). */
function parseNumeric(value: string): number {
  const match = value.match(/\d+/);
  return match ? parseInt(match[0], 10) : 0;
}

/** Big number metric card with count-up animation. */
export function MetricCard({ value, label, detail, index, isInView, className }: MetricCardProps) {
  const [displayValue, setDisplayValue] = useState('0');
  const hasAnimated = useRef(false);

  useEffect(() => {
    if (!isInView || hasAnimated.current) return;
    hasAnimated.current = true;

    const target = parseNumeric(value);
    const suffix = value.replace(/\d+/, '');
    const duration = 1200;
    const steps = 30;
    const stepTime = duration / steps;
    let current = 0;

    const interval = setInterval(() => {
      current += 1;
      const progress = current / steps;
      const eased = 1 - Math.pow(1 - progress, 3);
      const num = Math.round(eased * target);
      setDisplayValue(`${num}${suffix}`);
      if (current >= steps) {
        clearInterval(interval);
        setDisplayValue(value);
      }
    }, stepTime);

    return () => clearInterval(interval);
  }, [isInView, value]);

  return (
    <motion.div
      variants={{ hidden, visible: visible(index) }}
      className={cn(
        'group rounded-xl border-2 border-border/50 bg-card/50 p-6 text-center backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_24px_rgba(220,255,2,0.08)]',
        className,
      )}
    >
      <p className="text-4xl font-black tracking-tight text-primary sm:text-5xl">{displayValue}</p>
      <p className="mt-1 text-base font-bold">{label}</p>
      <p className="mt-0.5 text-sm text-muted-foreground">{detail}</p>
    </motion.div>
  );
}
