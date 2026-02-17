import type { ReactNode } from 'react';
import { motion } from 'motion/react';

interface StepCardProps {
  step: number;
  icon: ReactNode;
  title: string;
  description: string;
  index: number;
}

/** Numbered step card for the "How It Works" section. */
export function StepCard({ step, icon, title, description, index }: StepCardProps) {
  return (
    <motion.div
      variants={{
        hidden: { opacity: 0, y: 24 },
        visible: { opacity: 1, y: 0 },
      }}
      transition={{ duration: 0.5, delay: index * 0.15, ease: [0.25, 0.46, 0.45, 0.94] }}
      className="relative flex flex-col items-center text-center"
    >
      <div className="relative mb-4">
        <div className="flex size-16 items-center justify-center rounded-2xl border-2 border-primary/20 bg-primary/10 text-primary">
          {icon}
        </div>
        <span className="absolute -right-2 -top-2 flex size-7 items-center justify-center rounded-full bg-primary text-sm font-black text-primary-foreground">
          {step}
        </span>
      </div>
      <h3 className="mb-2 text-lg font-bold">{title}</h3>
      <p className="text-base leading-relaxed text-muted-foreground">{description}</p>
    </motion.div>
  );
}
