import type { ReactNode } from 'react';
import { motion } from 'motion/react';

interface SecurityCardProps {
  icon: ReactNode;
  title: string;
  description: string;
  index: number;
}

/** Security feature highlight card with icon and staggered entrance. */
export function SecurityCard({ icon, title, description, index }: SecurityCardProps) {
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: 24 },
        visible: { opacity: 1, y: 0 },
      }}
      transition={{ duration: 0.5, delay: index * 0.1, ease: [0.25, 0.46, 0.45, 0.94] }}
      className="flex gap-4 rounded-xl border border-border/40 bg-card/30 p-5 backdrop-blur-sm"
    >
      <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
        {icon}
      </div>
      <div>
        <h3 className="font-bold">{title}</h3>
        <p className="mt-1 text-base leading-relaxed text-muted-foreground">{description}</p>
      </div>
    </motion.div>
  );
}
