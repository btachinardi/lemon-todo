import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import { SectionHeading } from '../atoms/SectionHeading';

const ease = [0.25, 0.46, 0.45, 0.94] as const;
const hidden: Variant = { opacity: 0, y: 24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.6, delay: i * 0.15, ease },
});

interface StageConfig {
  name: string;
  cost: string;
  tagline: string;
  badge: string;
  resources: string[];
}

const badgeStyles: Record<string, string> = {
  CURRENT: 'bg-primary/20 text-primary border-primary/40',
  ACTUAL: 'bg-primary/20 text-primary border-primary/40',
  ATUAL: 'bg-primary/20 text-primary border-primary/40',
  NEXT: 'bg-muted text-muted-foreground border-border/50',
  SIGUIENTE: 'bg-muted text-muted-foreground border-border/50',
  'PRÓXIMO': 'bg-muted text-muted-foreground border-border/50',
  FUTURE: 'bg-muted/50 text-muted-foreground/70 border-border/30',
  FUTURO: 'bg-muted/50 text-muted-foreground/70 border-border/30',
};

/** Three-stage Terraform infrastructure comparison. */
export function DevOpsInfraSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const stages = t('devops.infra.stages', { returnObjects: true }) as StageConfig[];

  return (
    <section ref={ref} className="bg-muted/30 px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease }}
        >
          <SectionHeading
            title={t('devops.infra.title')}
            subtitle={t('devops.infra.subtitle')}
            highlight={t('devops.infra.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-8 lg:grid-cols-3"
        >
          {stages.map((stage, i) => (
            <motion.div
              key={i}
              variants={{ hidden, visible: visible(i) }}
              className={`group relative flex flex-col rounded-2xl border-2 p-8 backdrop-blur-sm transition-all duration-300 hover:shadow-[0_0_32px_rgba(220,255,2,0.1)] ${
                i === 0
                  ? 'border-primary/40 bg-card/60 shadow-[0_0_24px_rgba(220,255,2,0.06)]'
                  : 'border-border/50 bg-card/50 hover:border-primary/30'
              }`}
            >
              {/* Badge */}
              <span
                className={`mb-4 inline-flex w-fit rounded-full border px-3 py-0.5 text-[10px] font-bold uppercase tracking-widest ${
                  badgeStyles[stage.badge] || 'bg-muted text-muted-foreground border-border/50'
                }`}
              >
                {stage.badge}
              </span>

              {/* Header */}
              <h3 className="text-xl font-extrabold tracking-tight">{stage.name}</h3>
              <p className="mt-1 text-base text-muted-foreground">{stage.tagline}</p>

              {/* Cost */}
              <p className="mt-4 text-3xl font-black tracking-tight text-primary">{stage.cost}</p>
              <p className="mt-0.5 text-[10px] text-muted-foreground/60">est. monthly cloud infra</p>

              {/* Resources */}
              <ul className="mt-6 flex-1 space-y-2">
                {stage.resources.map((resource, j) => (
                  <li key={j} className="flex items-start gap-2 text-base">
                    <span className="mt-1.5 size-1.5 shrink-0 rounded-full bg-primary/60" />
                    <span className="text-muted-foreground">{resource}</span>
                  </li>
                ))}
              </ul>
            </motion.div>
          ))}
        </motion.div>

        <motion.p
          initial={{ opacity: 0 }}
          animate={isInView ? { opacity: 1 } : { opacity: 0 }}
          transition={{ duration: 0.6, delay: 0.6, ease }}
          className="mt-6 text-center text-sm text-muted-foreground/50"
        >
          {t('devops.infra.costNote')}
        </motion.p>
      </div>
    </section>
  );
}
