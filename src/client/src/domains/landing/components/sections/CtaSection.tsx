import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import { GithubIcon } from 'lucide-react';
import { GlowButton } from '../atoms/GlowButton';

/** Final call-to-action section with glowing button. */
export function CtaSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <motion.div
        initial={{ opacity: 0, y: 24 }}
        animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
        transition={{ duration: 0.7, ease: [0.25, 0.46, 0.45, 0.94] }}
        className="mx-auto max-w-3xl text-center"
      >
        <h2 className="text-3xl font-extrabold tracking-tight sm:text-4xl">
          {t('landing.cta.title')}
        </h2>
        <p className="mt-4 text-lg text-muted-foreground">{t('landing.cta.subtitle')}</p>
        <div className="mt-8 flex flex-wrap justify-center gap-4">
          <GlowButton variant="primary" to="/register">
            {t('landing.cta.ctaPrimary')}
          </GlowButton>
          <GlowButton
            variant="outline"
            href="https://github.com/btachinardi/lemon-todo"
            target="_blank"
            rel="noopener noreferrer"
          >
            <GithubIcon className="size-4" />
            {t('landing.cta.ctaGithub')}
          </GlowButton>
        </div>
      </motion.div>
    </section>
  );
}
