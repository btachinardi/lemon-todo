import type { ReactNode } from 'react';
import { motion, type Variant } from 'motion/react';
import { ExternalLinkIcon } from 'lucide-react';
import { cn } from '@/lib/utils';

interface JourneyCardProps {
  icon: ReactNode;
  title: string;
  subtitle: string;
  description: string;
  decision: string;
  tests: string;
  tag?: string;
  summary?: string;
  index: number;
  className?: string;
}

const hidden: Variant = { opacity: 0, x: -24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  x: 0,
  transition: { duration: 0.6, delay: i * 0.15, ease: [0.25, 0.46, 0.45, 0.94] },
});

/** Timeline milestone card for the story journey section. */
export function JourneyCard({
  icon,
  title,
  subtitle,
  description,
  decision,
  tests,
  tag,
  summary,
  index,
  className,
}: JourneyCardProps) {
  return (
    <motion.div
      variants={{ hidden, visible: visible(index) }}
      className={cn(
        'group relative rounded-xl border-2 border-border/50 bg-card/50 p-6 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_24px_rgba(220,255,2,0.08)]',
        className,
      )}
    >
      <div className="mb-3 flex items-center gap-3">
        <div className="flex size-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary transition-colors group-hover:bg-primary/15">
          {icon}
        </div>
        <div className="flex-1">
          <h3 className="text-lg font-bold">{title}</h3>
          <p className="text-sm font-medium text-primary">{subtitle}</p>
        </div>
        {tag && (
          <a
            href={`https://github.com/btachinardi/lemon-todo/releases/tag/${tag}`}
            target="_blank"
            rel="noopener noreferrer"
            className="flex shrink-0 items-center gap-1 rounded-full border border-border/40 bg-muted/30 px-2.5 py-1 text-xs font-semibold text-muted-foreground transition-colors hover:border-primary/40 hover:text-foreground"
          >
            {tag}
            <ExternalLinkIcon className="size-3" />
          </a>
        )}
      </div>
      <p className="mb-3 text-base leading-relaxed text-muted-foreground">{description}</p>
      {summary && (
        <p className="mb-3 text-sm leading-relaxed text-muted-foreground/70 italic">{summary}</p>
      )}
      <div className="flex items-center justify-between border-t border-border/30 pt-3">
        <p className="text-sm text-muted-foreground/80">{decision}</p>
        <span className="shrink-0 rounded-full bg-primary/10 px-2.5 py-0.5 text-sm font-bold text-primary">
          {tests} tests
        </span>
      </div>
    </motion.div>
  );
}
