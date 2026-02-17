import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import {
  ClipboardListIcon,
  HeartPulseIcon,
  ShieldCheckIcon,
  WifiOffIcon,
  CloudIcon,
  BarChart3Icon,
} from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';

const icons = [
  ClipboardListIcon,
  HeartPulseIcon,
  ShieldCheckIcon,
  WifiOffIcon,
  CloudIcon,
  BarChart3Icon,
];

const hidden: Variant = { opacity: 0, y: 24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.5, delay: i * 0.1, ease: [0.25, 0.46, 0.45, 0.94] },
});

interface RationaleStep {
  title: string;
  description: string;
}

/** Assignment rationale section — the reasoning chain from prompt to feature scope. */
export function StoryRationaleSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const steps = t('story.rationale.steps', { returnObjects: true }) as RationaleStep[];

  return (
    <section ref={ref} className="bg-muted/30 px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('story.rationale.title')}
            subtitle={t('story.rationale.subtitle')}
            highlight={t('story.rationale.highlight')}
          />
        </motion.div>

        <div className="mt-16 grid items-start gap-12 lg:grid-cols-5">
          {/* Narrative column */}
          <motion.div
            initial={{ opacity: 0, y: 24 }}
            animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
            transition={{ duration: 0.6, delay: 0.15, ease: [0.25, 0.46, 0.45, 0.94] }}
            className="lg:col-span-2"
          >
            <div className="rounded-xl border-2 border-primary/20 bg-card/50 p-6 backdrop-blur-sm">
              <p className="text-base font-semibold uppercase tracking-wider text-primary">
                {t('story.rationale.promptLabel')}
              </p>
              <blockquote className="mt-3 border-l-2 border-primary/40 pl-4 text-base italic leading-relaxed text-muted-foreground">
                {t('story.rationale.prompt')}
              </blockquote>
              <p className="mt-6 text-base leading-relaxed text-muted-foreground">
                {t('story.rationale.interpretation')}
              </p>
            </div>
          </motion.div>

          {/* Reasoning chain cards */}
          <motion.div
            initial="hidden"
            animate={isInView ? 'visible' : 'hidden'}
            className="grid gap-4 sm:grid-cols-2 lg:col-span-3"
          >
            {steps.map((step, i) => {
              const Icon = icons[i] ?? ClipboardListIcon;
              return (
                <motion.div
                  key={i}
                  variants={{ hidden, visible: visible(i) }}
                  className="group flex gap-3 rounded-xl border-2 border-border/50 bg-card/50 p-4 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_24px_rgba(220,255,2,0.08)]"
                >
                  <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary transition-colors group-hover:bg-primary/15">
                    <Icon className="size-4" />
                  </div>
                  <div className="min-w-0">
                    <h3 className="text-base font-bold leading-tight">{step.title}</h3>
                    <p className="mt-1 text-sm leading-relaxed text-muted-foreground">
                      {step.description}
                    </p>
                  </div>
                </motion.div>
              );
            })}
          </motion.div>
        </div>
      </div>
    </section>
  );
}
