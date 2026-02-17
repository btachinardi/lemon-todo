import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import { GithubIcon, ScaleIcon, CodeIcon } from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';

/** Open source pitch with GitHub badge, MIT license, and self-host messaging. */
export function OpenSourceSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const badges = [
    { icon: GithubIcon, label: t('landing.openSource.badge1') },
    { icon: ScaleIcon, label: t('landing.openSource.badge2') },
    { icon: CodeIcon, label: t('landing.openSource.badge3') },
  ];

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease: [0.25, 0.46, 0.45, 0.94] }}
        >
          <SectionHeading
            title={t('landing.openSource.title')}
            subtitle={t('landing.openSource.subtitle')}
            highlight={t('landing.openSource.highlight')}
          />
        </motion.div>

        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-12 flex flex-col items-center gap-8"
        >
          <motion.div
            variants={{ hidden: { opacity: 0, y: 16 }, visible: { opacity: 1, y: 0 } }}
            transition={{ duration: 0.5, delay: 0.2 }}
            className="flex flex-wrap justify-center gap-4"
          >
            {badges.map((b, i) => (
              <div
                key={i}
                className="flex items-center gap-2 rounded-full border border-border/50 bg-card/40 px-4 py-2 text-sm font-medium backdrop-blur-sm"
              >
                <b.icon className="size-4 text-primary" />
                {b.label}
              </div>
            ))}
          </motion.div>

          <motion.div
            variants={{ hidden: { opacity: 0, y: 16 }, visible: { opacity: 1, y: 0 } }}
            transition={{ duration: 0.5, delay: 0.35 }}
            className="max-w-2xl space-y-3 text-center"
          >
            <p className="text-lg font-semibold">{t('landing.openSource.pitch')}</p>
            <p className="text-muted-foreground">{t('landing.openSource.noVendorLock')}</p>
          </motion.div>

          <motion.a
            variants={{ hidden: { opacity: 0, scale: 0.95 }, visible: { opacity: 1, scale: 1 } }}
            transition={{ duration: 0.4, delay: 0.45 }}
            href="https://github.com/btachinardi/lemon-todo"
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex items-center gap-2 rounded-lg border-2 border-border/50 bg-card/40 px-5 py-3 font-bold transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_20px_rgba(220,255,2,0.1)]"
          >
            <GithubIcon className="size-5" />
            {t('landing.openSource.viewGithub')}
          </motion.a>
        </motion.div>
      </div>
    </section>
  );
}
