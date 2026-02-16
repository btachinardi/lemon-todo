import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import { ArrowDownIcon } from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';

const hidden: Variant = { opacity: 0, y: 24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.5, delay: i * 0.12, ease: [0.25, 0.46, 0.45, 0.94] },
});

/** Architecture overview showing backend layers and frontend tiers. */
export function StoryArchSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const backendLayers = t('story.arch.backendLayers', { returnObjects: true }) as string[];
  const frontendTiers = t('story.arch.frontendTiers', { returnObjects: true }) as string[];
  const decisions = t('story.arch.decisions', { returnObjects: true }) as string[];

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('story.arch.title')}
            subtitle={t('story.arch.subtitle')}
            highlight={t('story.arch.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-12 lg:grid-cols-2"
        >
          {/* Backend layers */}
          <motion.div variants={{ hidden, visible: visible(0) }}>
            <h3 className="mb-6 text-center text-xs font-bold uppercase tracking-widest text-primary">
              {t('story.arch.backendLabel')}
            </h3>
            <div className="mx-auto flex max-w-sm flex-col items-center gap-2">
              {backendLayers.map((layer, i) => (
                <div key={layer} className="flex w-full flex-col items-center">
                  <div
                    className="flex h-12 items-center justify-center rounded-lg border-2 border-border/50 bg-card/50 font-bold transition-colors hover:border-primary/30"
                    style={{ width: `${100 - i * 8}%` }}
                  >
                    {layer}
                  </div>
                  {i < backendLayers.length - 1 && (
                    <ArrowDownIcon className="my-1 size-4 text-muted-foreground/50" />
                  )}
                </div>
              ))}
            </div>
          </motion.div>

          {/* Frontend tiers */}
          <motion.div variants={{ hidden, visible: visible(1) }}>
            <h3 className="mb-6 text-center text-xs font-bold uppercase tracking-widest text-primary">
              {t('story.arch.frontendLabel')}
            </h3>
            <div className="mx-auto flex max-w-sm flex-col items-center gap-2">
              {frontendTiers.map((tier, i) => (
                <div key={tier} className="flex w-full flex-col items-center">
                  <div
                    className="flex h-12 items-center justify-center rounded-lg border-2 border-border/50 bg-card/50 font-bold transition-colors hover:border-primary/30"
                    style={{ width: `${100 - i * 8}%` }}
                  >
                    {tier}
                  </div>
                  {i < frontendTiers.length - 1 && (
                    <ArrowDownIcon className="my-1 size-4 text-muted-foreground/50" />
                  )}
                </div>
              ))}
            </div>
          </motion.div>
        </motion.div>

        {/* Key decisions */}
        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-12 flex flex-wrap justify-center gap-3"
        >
          {decisions.map((text, i) => (
            <motion.span
              key={i}
              variants={{ hidden, visible: visible(i + 2) }}
              className="rounded-full border border-border/40 bg-card/40 px-4 py-2 text-xs font-medium text-muted-foreground backdrop-blur-sm"
            >
              {text}
            </motion.span>
          ))}
        </motion.div>
      </div>
    </section>
  );
}
