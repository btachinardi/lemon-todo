import type { ReactNode } from 'react';
import { motion } from 'motion/react';
import { cn } from '@/lib/utils';

interface FeatureCardProps {
  icon: ReactNode;
  title: string;
  description: string;
  index: number;
  className?: string;
}

/** Feature highlight card with icon, hover glow, and staggered entrance animation. */
export function FeatureCard({ icon, title, description, index, className }: FeatureCardProps) {
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: 24 },
        visible: { opacity: 1, y: 0 },
      }}
      transition={{ duration: 0.5, delay: index * 0.1, ease: [0.25, 0.46, 0.45, 0.94] }}
      className={cn(
        'group relative rounded-xl border-2 border-border/50 bg-card/50 p-6 backdrop-blur-sm',
        'transition-all duration-300',
        'hover:border-primary/30 hover:shadow-[0_0_24px_rgba(220,255,2,0.08)]',
        className,
      )}
    >
      <div className="mb-4 inline-flex rounded-lg bg-primary/10 p-2.5 text-primary transition-colors group-hover:bg-primary/15">
        {icon}
      </div>
      <h3 className="mb-2 text-lg font-bold">{title}</h3>
      <p className="text-sm leading-relaxed text-muted-foreground">{description}</p>
    </motion.div>
  );
}
