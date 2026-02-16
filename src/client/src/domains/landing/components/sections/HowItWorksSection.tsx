import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import { ContainerIcon, LayoutDashboardIcon, ShieldCheckIcon } from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';
import { StepCard } from '../widgets/StepCard';

const stepIcons = [ContainerIcon, LayoutDashboardIcon, ShieldCheckIcon];

/** 3-step "How It Works" section: Deploy, Create, Stay Compliant. */
export function HowItWorksSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const steps = stepIcons.map((Icon, i) => ({
    icon: <Icon className="size-7" />,
    title: t(`landing.howItWorks.steps.${i}.title`),
    description: t(`landing.howItWorks.steps.${i}.description`),
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
            title={t('landing.howItWorks.title')}
            subtitle={t('landing.howItWorks.subtitle')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-12 sm:grid-cols-3 sm:gap-8"
        >
          {steps.map((s, i) => (
            <StepCard
              key={i}
              step={i + 1}
              icon={s.icon}
              title={s.title}
              description={s.description}
              index={i}
            />
          ))}
        </motion.div>
      </div>
    </section>
  );
}
