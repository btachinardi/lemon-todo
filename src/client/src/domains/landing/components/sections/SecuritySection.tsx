import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import { ServerIcon, ClipboardListIcon, ShieldIcon, BuildingIcon } from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';
import { SecurityCard } from '../widgets/SecurityCard';

const securityIcons = [ServerIcon, ClipboardListIcon, ShieldIcon, BuildingIcon];

/** Security features grid: data sovereignty, audit trail, RBAC, compliance. */
export function SecuritySection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const items = securityIcons.map((Icon, i) => ({
    icon: <Icon className="size-5" />,
    title: t(`landing.security.items.${i}.title`),
    description: t(`landing.security.items.${i}.description`),
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
            title={t('landing.security.title')}
            subtitle={t('landing.security.subtitle')}
            highlight={t('landing.security.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-12 grid gap-4 sm:grid-cols-2"
        >
          {items.map((item, i) => (
            <SecurityCard
              key={i}
              icon={item.icon}
              title={item.title}
              description={item.description}
              index={i}
            />
          ))}
        </motion.div>
      </div>
    </section>
  );
}
