import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView } from 'motion/react';
import { GithubIcon } from 'lucide-react';
import { GlowButton } from '../atoms/GlowButton';

const ease = [0.25, 0.46, 0.45, 0.94] as const;

/** DevOps page final CTA section. */
export function DevOpsCtaSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  return (
    <section ref={ref} className="bg-muted/30 px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-3xl text-center">
        <motion.h2
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease }}
          className="text-3xl font-extrabold tracking-tight sm:text-4xl lg:text-5xl"
        >
          {t('devops.cta.title')}
        </motion.h2>
        <motion.p
          initial={{ opacity: 0, y: 16 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 16 }}
          transition={{ duration: 0.6, delay: 0.15, ease }}
          className="mt-4 text-base text-muted-foreground sm:text-lg"
        >
          {t('devops.cta.subtitle')}
        </motion.p>
        <motion.div
          initial={{ opacity: 0, y: 20 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 20 }}
          transition={{ duration: 0.6, delay: 0.3, ease }}
          className="mt-8 flex flex-wrap justify-center gap-4"
        >
          <GlowButton variant="primary" to="/register">
            {t('devops.cta.ctaPrimary')}
          </GlowButton>
          <GlowButton
            variant="outline"
            href="https://github.com/btachinardi/lemon-todo/tree/main/infra"
            target="_blank"
            rel="noopener noreferrer"
          >
            <GithubIcon className="size-4" />
            {t('devops.cta.ctaGithub')}
          </GlowButton>
        </motion.div>
      </div>
    </section>
  );
}
