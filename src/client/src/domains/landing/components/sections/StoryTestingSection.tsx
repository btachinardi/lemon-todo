import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import {
  DicesIcon,
  DatabaseIcon,
  CameraIcon,
  ContrastIcon,
  UsersIcon,
  FileCodeIcon,
} from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';

const icons = [DicesIcon, DatabaseIcon, CameraIcon, ContrastIcon, UsersIcon, FileCodeIcon];

const hidden: Variant = { opacity: 0, y: 24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.6, delay: i * 0.1, ease: [0.25, 0.46, 0.45, 0.94] },
});

interface TestingItem {
  title: string;
  description: string;
}

/** Testing discipline section — diverse strategies beyond line coverage. */
export function StoryTestingSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const items = t('story.testing.items', { returnObjects: true }) as TestingItem[];

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('story.testing.title')}
            subtitle={t('story.testing.subtitle')}
            highlight={t('story.testing.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-6 sm:grid-cols-2 lg:grid-cols-3"
        >
          {items.map((item, i) => {
            const Icon = icons[i] ?? DicesIcon;
            return (
              <motion.div
                key={i}
                variants={{ hidden, visible: visible(i) }}
                className="group rounded-xl border-2 border-border/50 bg-card/50 p-6 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_24px_rgba(220,255,2,0.08)]"
              >
                <div className="mb-4 flex size-11 items-center justify-center rounded-lg bg-primary/10 text-primary transition-colors group-hover:bg-primary/15">
                  <Icon className="size-5" />
                </div>
                <h3 className="text-base font-bold leading-tight">{item.title}</h3>
                <p className="mt-2 text-base leading-relaxed text-muted-foreground">
                  {item.description}
                </p>
              </motion.div>
            );
          })}
        </motion.div>
      </div>
    </section>
  );
}
