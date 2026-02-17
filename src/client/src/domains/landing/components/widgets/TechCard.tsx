import type { ReactNode } from 'react';
import { motion, type Variant } from 'motion/react';
import { cn } from '@/lib/utils';

interface TechCardProps {
  icon: ReactNode;
  name: string;
  description: string;
  index: number;
  className?: string;
}

const hidden: Variant = { opacity: 0, y: 16 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.5, delay: i * 0.08, ease: [0.25, 0.46, 0.45, 0.94] },
});

/** Technology item card with icon, name, and description. */
export function TechCard({ icon, name, description, index, className }: TechCardProps) {
  return (
    <motion.div
      variants={{ hidden, visible: visible(index) }}
      className={cn(
        'group flex items-center gap-3 rounded-lg border border-border/40 bg-card/40 px-4 py-3 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_16px_rgba(220,255,2,0.06)]',
        className,
      )}
    >
      <div className="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary transition-colors group-hover:bg-primary/15">
        {icon}
      </div>
      <div className="min-w-0">
        <p className="text-sm font-bold">{name}</p>
        <p className="truncate text-xs text-muted-foreground">{description}</p>
      </div>
    </motion.div>
  );
}
