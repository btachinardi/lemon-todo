import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import type { LucideIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
import { SectionHeading } from '../atoms/SectionHeading';
import { TierCard } from '../widgets/TierCard';

interface TierData {
  icon: LucideIcon;
  featureIcons: LucideIcon[];
}

interface RoadmapHorizonSectionProps {
  horizonKey: 'near' | 'mid' | 'far';
  tiers: TierData[];
  tierStartIndex: number;
  className?: string;
}

/** Horizon section grouping related roadmap tiers with a labeled heading. */
export function RoadmapHorizonSection({
  horizonKey,
  tiers,
  tierStartIndex,
  className,
}: RoadmapHorizonSectionProps) {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  return (
    <section ref={ref} className={cn('px-4 py-24 sm:px-6', className)}>
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <div className="mb-4 text-center">
            <span className="inline-block rounded-full border border-primary/30 bg-primary/10 px-4 py-1.5 text-sm font-black tracking-widest text-primary">
              {t(`roadmap.horizons.${horizonKey}.label`)}
            </span>
          </div>
          <SectionHeading
            title={t(`roadmap.horizons.${horizonKey}.title`)}
            subtitle={t(`roadmap.horizons.${horizonKey}.subtitle`)}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-8 lg:grid-cols-2"
        >
          {tiers.map((tier, i) => (
            <TierCard
              key={tierStartIndex + i}
              tierIndex={tierStartIndex + i}
              icon={tier.icon}
              featureIcons={tier.featureIcons}
              index={i}
            />
          ))}
        </motion.div>
      </div>
    </section>
  );
}
