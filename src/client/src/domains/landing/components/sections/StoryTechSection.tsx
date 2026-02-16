import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import {
  ServerIcon,
  CloudIcon,
  DatabaseIcon,
  HardDriveIcon,
  ComponentIcon,
  ZapIcon,
  PaintbrushIcon,
  LayoutGridIcon,
  RefreshCwIcon,
  BoxIcon,
  FlaskConicalIcon,
  ShuffleIcon,
  TestTubeIcon,
  DicesIcon,
  MonitorPlayIcon,
  GlobeIcon,
  BlocksIcon,
  GitBranchIcon,
  ContainerIcon,
  LanguagesIcon,
  Wand2Icon,
  GripVerticalIcon,
  WifiOffIcon,
} from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';
import { TechCard } from '../widgets/TechCard';

interface TechItem {
  key: string;
  icon: React.ReactNode;
}

const categories: { key: string; items: TechItem[] }[] = [
  {
    key: 'backend',
    items: [
      { key: 'dotnet', icon: <ServerIcon className="size-4" /> },
      { key: 'aspire', icon: <CloudIcon className="size-4" /> },
      { key: 'efcore', icon: <DatabaseIcon className="size-4" /> },
      { key: 'sqlite', icon: <HardDriveIcon className="size-4" /> },
    ],
  },
  {
    key: 'frontend',
    items: [
      { key: 'react', icon: <ComponentIcon className="size-4" /> },
      { key: 'vite', icon: <ZapIcon className="size-4" /> },
      { key: 'tailwind', icon: <PaintbrushIcon className="size-4" /> },
      { key: 'shadcn', icon: <LayoutGridIcon className="size-4" /> },
      { key: 'tanstack', icon: <RefreshCwIcon className="size-4" /> },
      { key: 'zustand', icon: <BoxIcon className="size-4" /> },
      { key: 'i18next', icon: <LanguagesIcon className="size-4" /> },
      { key: 'motion', icon: <Wand2Icon className="size-4" /> },
      { key: 'dndkit', icon: <GripVerticalIcon className="size-4" /> },
    ],
  },
  {
    key: 'testing',
    items: [
      { key: 'mstest', icon: <FlaskConicalIcon className="size-4" /> },
      { key: 'fscheck', icon: <ShuffleIcon className="size-4" /> },
      { key: 'vitest', icon: <TestTubeIcon className="size-4" /> },
      { key: 'fastcheck', icon: <DicesIcon className="size-4" /> },
      { key: 'playwright', icon: <MonitorPlayIcon className="size-4" /> },
    ],
  },
  {
    key: 'devops',
    items: [
      { key: 'azure', icon: <GlobeIcon className="size-4" /> },
      { key: 'terraform', icon: <BlocksIcon className="size-4" /> },
      { key: 'ghactions', icon: <GitBranchIcon className="size-4" /> },
      { key: 'docker', icon: <ContainerIcon className="size-4" /> },
      { key: 'workbox', icon: <WifiOffIcon className="size-4" /> },
    ],
  },
];

/** Technology stack grid organized by category. */
export function StoryTechSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  let globalIndex = 0;

  return (
    <section ref={ref} className="bg-muted/30 px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('story.tech.title')}
            subtitle={t('story.tech.subtitle')}
            highlight={t('story.tech.highlight')}
          />
        </motion.div>

        <div className="mt-16 space-y-10">
          {categories.map((cat) => (
            <div key={cat.key}>
              <h3 className="mb-4 text-xs font-bold uppercase tracking-widest text-primary">
                {t(`story.tech.categories.${cat.key}`)}
              </h3>
              <motion.div
                initial="hidden"
                animate={isInView ? 'visible' : 'hidden'}
                className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4"
              >
                {cat.items.map((item) => {
                  const idx = globalIndex++;
                  return (
                    <TechCard
                      key={item.key}
                      icon={item.icon}
                      name={t(`story.tech.items.${item.key}.name`)}
                      description={t(`story.tech.items.${item.key}.description`)}
                      index={idx}
                    />
                  );
                })}
              </motion.div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
