import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import {
  ContainerIcon,
  DatabaseIcon,
  KeyRoundIcon,
  ActivityIcon,
  GlobeIcon,
  NetworkIcon,
  ShieldCheckIcon,
  ZapIcon,
  HardDriveIcon,
} from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';

const ease = [0.25, 0.46, 0.45, 0.94] as const;
const hidden: Variant = { opacity: 0, y: 16 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.5, delay: i * 0.08, ease },
});

const moduleIcons = [
  ContainerIcon,
  DatabaseIcon,
  KeyRoundIcon,
  ActivityIcon,
  GlobeIcon,
  NetworkIcon,
  ShieldCheckIcon,
  ZapIcon,
  HardDriveIcon,
];

interface ModuleItem {
  name: string;
  description: string;
}

/** Grid of 9 reusable Terraform modules. */
export function DevOpsModulesSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const items = t('devops.modules.items', { returnObjects: true }) as ModuleItem[];

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease }}
        >
          <SectionHeading
            title={t('devops.modules.title')}
            subtitle={t('devops.modules.subtitle')}
            highlight={t('devops.modules.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-3 sm:grid-cols-2 lg:grid-cols-3"
        >
          {items.map((item, i) => {
            const Icon = moduleIcons[i] || ContainerIcon;
            return (
              <motion.div
                key={item.name}
                variants={{ hidden, visible: visible(i) }}
                className="group flex items-center gap-3 rounded-lg border border-border/40 bg-card/40 px-4 py-3 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_16px_rgba(220,255,2,0.06)]"
              >
                <div className="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary transition-colors group-hover:bg-primary/15">
                  <Icon className="size-4" />
                </div>
                <div className="min-w-0">
                  <p className="font-mono text-sm font-bold">{item.name}</p>
                  <p className="truncate text-xs text-muted-foreground">{item.description}</p>
                </div>
              </motion.div>
            );
          })}
        </motion.div>
      </div>
    </section>
  );
}
