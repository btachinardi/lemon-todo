import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import { ExternalLinkIcon } from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';

const ease = [0.25, 0.46, 0.45, 0.94] as const;
const hidden: Variant = { opacity: 0, y: 16 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.5, delay: i * 0.08, ease },
});

interface LiveComponent {
  name: string;
  detail: string;
}

/** What's running right now in Azure — live infrastructure status. */
export function DevOpsLiveSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const components = t('devops.live.components', { returnObjects: true }) as LiveComponent[];

  return (
    <section ref={ref} className="bg-muted/30 px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease }}
        >
          <SectionHeading
            title={t('devops.live.title')}
            subtitle={t('devops.live.subtitle')}
            highlight={t('devops.live.highlight')}
          />
        </motion.div>

        {/* Live domain badges */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 16 }}
          transition={{ duration: 0.6, delay: 0.2, ease }}
          className="mt-10 flex flex-wrap justify-center gap-4"
        >
          <a
            href={`https://${t('devops.live.domains.frontend')}`}
            target="_blank"
            rel="noopener noreferrer"
            className="group inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-4 py-2 text-base font-bold text-primary transition-all hover:bg-primary/20 hover:shadow-[0_0_16px_rgba(220,255,2,0.15)]"
          >
            <span className="relative flex size-2">
              <span className="absolute inline-flex size-full animate-ping rounded-full bg-primary/60" />
              <span className="relative inline-flex size-2 rounded-full bg-primary" />
            </span>
            {t('devops.live.domains.frontend')}
            <ExternalLinkIcon className="size-3 opacity-60 transition-opacity group-hover:opacity-100" />
          </a>
          <a
            href={`https://${t('devops.live.domains.api')}/health`}
            target="_blank"
            rel="noopener noreferrer"
            className="group inline-flex items-center gap-2 rounded-full border border-primary/30 bg-primary/10 px-4 py-2 text-base font-bold text-primary transition-all hover:bg-primary/20 hover:shadow-[0_0_16px_rgba(220,255,2,0.15)]"
          >
            <span className="relative flex size-2">
              <span className="absolute inline-flex size-full animate-ping rounded-full bg-primary/60" />
              <span className="relative inline-flex size-2 rounded-full bg-primary" />
            </span>
            {t('devops.live.domains.api')}
            <ExternalLinkIcon className="size-3 opacity-60 transition-opacity group-hover:opacity-100" />
          </a>
        </motion.div>

        {/* Component grid */}
        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-12 grid gap-4 sm:grid-cols-2 lg:grid-cols-4"
        >
          {components.map((comp, i) => (
            <motion.div
              key={i}
              variants={{ hidden, visible: visible(i) }}
              className="group rounded-xl border border-border/40 bg-card/40 p-5 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_16px_rgba(220,255,2,0.06)]"
            >
              <div className="mb-2 flex items-center gap-2">
                <span className="relative flex size-2">
                  <span className="absolute inline-flex size-full animate-ping rounded-full bg-success-foreground/60" />
                  <span className="relative inline-flex size-2 rounded-full bg-success-foreground" />
                </span>
                <h3 className="text-base font-bold">{comp.name}</h3>
              </div>
              <p className="text-sm leading-relaxed text-muted-foreground">{comp.detail}</p>
            </motion.div>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
