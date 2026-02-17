import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import { GithubIcon } from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';
import { GlowButton } from '../atoms/GlowButton';
import { MetricCard } from '../widgets/MetricCard';

interface MetricItem {
  value: string;
  label: string;
  detail: string;
}

/** Metrics wall with count-up animation and final CTA. */
export function StoryNumbersSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const items = t('story.numbers.items', { returnObjects: true }) as MetricItem[];

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('story.numbers.title')}
            highlight={t('story.numbers.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-6 sm:grid-cols-2 lg:grid-cols-3"
        >
          {items.map((item, i) => (
            <MetricCard
              key={i}
              value={item.value}
              label={item.label}
              detail={item.detail}
              index={i}
              isInView={isInView}
            />
          ))}
        </motion.div>

        {/* Final CTA */}
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 20 }}
          transition={{ duration: 0.6, delay: 0.8, ease: [0.25, 0.46, 0.45, 0.94] }}
          className="mt-16 flex flex-wrap justify-center gap-4"
        >
          <GlowButton variant="primary" to="/register">
            {t('story.numbers.ctaPrimary')}
          </GlowButton>
          <GlowButton
            variant="outline"
            href="https://github.com/btachinardi/lemon-todo"
            target="_blank"
            rel="noopener noreferrer"
          >
            <GithubIcon className="size-4" />
            {t('story.numbers.ctaGithub')}
          </GlowButton>
        </motion.div>
      </div>
    </section>
  );
}
