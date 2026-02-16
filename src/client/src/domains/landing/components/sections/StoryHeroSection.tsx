import { useTranslation } from 'react-i18next';
import { motion } from 'motion/react';
import { GithubIcon } from 'lucide-react';
import { GlowButton } from '../atoms/GlowButton';

const ease = [0.25, 0.46, 0.45, 0.94] as const;

/** Story page hero with terminal mockup showing the development journey. */
export function StoryHeroSection() {
  const { t } = useTranslation();

  return (
    <section className="relative flex min-h-[85vh] items-center overflow-hidden px-4 pt-24 sm:px-6">
      {/* Geometric background accents */}
      <div className="pointer-events-none absolute inset-0 overflow-hidden" aria-hidden="true">
        <div className="absolute -right-32 -top-32 size-96 rounded-full bg-primary/5 blur-3xl" />
        <div className="absolute -bottom-48 -left-48 size-[500px] rounded-full bg-primary/3 blur-3xl" />
        <div className="absolute left-1/2 top-1/4 size-px -translate-x-1/2">
          <div className="absolute inset-0 size-[600px] -translate-x-1/2 -translate-y-1/2 rounded-full border border-primary/[0.04]" />
          <div className="absolute inset-0 size-[900px] -translate-x-1/2 -translate-y-1/2 rounded-full border border-primary/[0.03]" />
        </div>
      </div>

      <div className="relative mx-auto grid w-full max-w-7xl items-center gap-12 lg:grid-cols-2 lg:gap-16">
        {/* Text content */}
        <div>
          <motion.h1
            initial={{ opacity: 0, y: 32 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, ease }}
            className="text-4xl font-extrabold leading-[1.1] tracking-tight sm:text-5xl lg:text-6xl"
          >
            {t('story.hero.titleLine1')}{' '}
            <span className="text-lemon glow-text">{t('story.hero.titleHighlight')}</span>{' '}
            {t('story.hero.titleLine2')}
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.15, ease }}
            className="mt-6 max-w-xl text-base leading-relaxed text-muted-foreground sm:text-lg"
          >
            {t('story.hero.subtitle')}
          </motion.p>
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.3, ease }}
            className="mt-8 flex flex-wrap gap-4"
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

        {/* Terminal mockup */}
        <motion.div
          initial={{ opacity: 0, scale: 0.95 }}
          animate={{ opacity: 1, scale: 1 }}
          transition={{ duration: 0.8, delay: 0.2, ease }}
          className="hidden lg:block"
          aria-hidden="true"
        >
          <div className="rounded-2xl border-2 border-border/40 bg-card/60 p-6 shadow-2xl shadow-primary/5 backdrop-blur-md">
            <div className="mb-4 flex items-center gap-2">
              <div className="size-3 rounded-full bg-destructive/60" />
              <div className="size-3 rounded-full bg-warning-foreground/40" />
              <div className="size-3 rounded-full bg-success-foreground/40" />
              <span className="ml-2 text-xs text-muted-foreground">terminal</span>
            </div>
            <div className="space-y-2 font-mono text-xs leading-relaxed sm:text-sm">
              <p>
                <span className="text-primary">$</span>{' '}
                <span className="text-muted-foreground">git init lemon-todo</span>
              </p>
              <p className="text-muted-foreground/60">Initialized empty Git repository</p>
              <p className="mt-3 text-muted-foreground/40">... 3 days later ...</p>
              <p className="mt-3">
                <span className="text-primary">$</span>{' '}
                <span className="text-muted-foreground">./dev verify</span>
              </p>
              <p>
                <span className="text-success-foreground">✓</span>{' '}
                <span className="text-muted-foreground">Backend Build</span>{' '}
                <span className="text-foreground/60">11 projects, 0 warnings</span>
              </p>
              <p>
                <span className="text-success-foreground">✓</span>{' '}
                <span className="text-muted-foreground">Frontend Build</span>{' '}
                <span className="text-foreground/60">432 KB JS + 48 KB CSS</span>
              </p>
              <p>
                <span className="text-success-foreground">✓</span>{' '}
                <span className="text-muted-foreground">Backend Tests</span>{' '}
                <span className="text-primary font-bold">370 passed</span>
              </p>
              <p>
                <span className="text-success-foreground">✓</span>{' '}
                <span className="text-muted-foreground">Frontend Tests</span>{' '}
                <span className="text-primary font-bold">243 passed</span>
              </p>
              <p>
                <span className="text-success-foreground">✓</span>{' '}
                <span className="text-muted-foreground">E2E Tests</span>{' '}
                <span className="text-primary font-bold">55 passed</span>
              </p>
              <p>
                <span className="text-success-foreground">✓</span>{' '}
                <span className="text-muted-foreground">Lint</span>{' '}
                <span className="text-foreground/60">Clean</span>
              </p>
              <p className="mt-2 text-primary font-bold">All checks passed.</p>
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
