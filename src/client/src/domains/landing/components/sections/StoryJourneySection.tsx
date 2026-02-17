import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import {
  LayoutDashboardIcon,
  ShieldCheckIcon,
  PaletteIcon,
  RocketIcon,
  SparklesIcon,
} from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';
import { JourneyCard } from '../widgets/JourneyCard';

const checkpointIcons = [
  LayoutDashboardIcon,
  ShieldCheckIcon,
  PaletteIcon,
  RocketIcon,
  SparklesIcon,
];

/** Vertical timeline of the 5 development checkpoints. */
export function StoryJourneySection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  return (
    <section ref={ref} className="bg-muted/30 px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-5xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('story.journey.title')}
            subtitle={t('story.journey.subtitle')}
            highlight={t('story.journey.highlight')}
          />
        </motion.div>

        {/* Timeline */}
        <div className="relative mt-16">
          {/* Center line (desktop only) */}
          <div className="absolute left-4 top-0 hidden h-full w-px bg-border/50 md:left-1/2 md:block" />

          <motion.div
            initial="hidden"
            animate={isInView ? 'visible' : 'hidden'}
            className="space-y-8 md:space-y-12"
          >
            {checkpointIcons.map((Icon, i) => (
              <div
                key={i}
                className={`relative flex items-start md:items-center ${
                  i % 2 === 0 ? 'md:justify-start' : 'md:justify-end'
                }`}
              >
                {/* Timeline dot */}
                <div className="absolute left-4 top-6 z-10 hidden size-3 -translate-x-1/2 rounded-full border-2 border-primary bg-background md:left-1/2 md:block" />

                <div className={`w-full md:w-[calc(50%-2rem)] ${i % 2 === 1 ? 'md:ml-auto' : ''}`}>
                  <JourneyCard
                    icon={<Icon className="size-5" />}
                    title={t(`story.journey.checkpoints.${i}.title`)}
                    subtitle={t(`story.journey.checkpoints.${i}.subtitle`)}
                    description={t(`story.journey.checkpoints.${i}.description`)}
                    decision={t(`story.journey.checkpoints.${i}.decision`)}
                    tests={t(`story.journey.checkpoints.${i}.tests`)}
                    tag={t(`story.journey.checkpoints.${i}.tag`)}
                    summary={t(`story.journey.checkpoints.${i}.summary`)}
                    index={i}
                  />
                </div>
              </div>
            ))}
          </motion.div>
        </div>



      </div>
    </section>
  );
}
