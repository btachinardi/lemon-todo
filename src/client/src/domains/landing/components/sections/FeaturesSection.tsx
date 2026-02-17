import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import {
  KanbanIcon,
  WifiOffIcon,
  BellRingIcon,
  ArrowUpCircleIcon,
  GlobeIcon,
  SparklesIcon,
  LockKeyholeIcon,
  SmartphoneIcon,
  MoonStarIcon,
} from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';
import { FeatureCard } from '../widgets/FeatureCard';

const featureIcons = [
  KanbanIcon,
  WifiOffIcon,
  BellRingIcon,
  ArrowUpCircleIcon,
  GlobeIcon,
  SparklesIcon,
  LockKeyholeIcon,
  SmartphoneIcon,
  MoonStarIcon,
];

/** 9-card feature grid highlighting core capabilities across all checkpoints. */
export function FeaturesSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const features = featureIcons.map((Icon, i) => ({
    icon: <Icon className="size-5" />,
    title: t(`landing.features.items.${i}.title`),
    description: t(`landing.features.items.${i}.description`),
  }));

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('landing.features.title')}
            subtitle={t('landing.features.subtitle')}
            highlight={t('landing.features.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-6 sm:grid-cols-2 lg:grid-cols-3"
        >
          {features.map((f, i) => (
            <FeatureCard
              key={i}
              icon={f.icon}
              title={f.title}
              description={f.description}
              index={i}
            />
          ))}
        </motion.div>
      </div>
    </section>
  );
}
