import { useTranslation } from 'react-i18next';
import { motion } from 'motion/react';
import { GithubIcon } from 'lucide-react';
import { GlowButton } from '../atoms/GlowButton';

const ease = [0.25, 0.46, 0.45, 0.94] as const;

const checkpoints = ['Core Tasks', 'Auth & Security', 'Rich UX', 'Production Ready', 'Delight'];
const upcomingTiers = [
  'AI & Agents',
  'Integrations',
  'Collaboration',
  'Advanced Tasks',
  'Developer DX',
  'Platform',
  'Growth',
  'UX Excellence',
  'Reliability',
];

/** Bold hero section with headline and stylized roadmap timeline preview. */
export function RoadmapHeroSection() {
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
            {t('roadmap.hero.titleLine1')}{' '}
            <span className="text-highlight glow-text">{t('roadmap.hero.titleHighlight')}</span>
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.15, ease }}
            className="mt-6 max-w-xl text-base leading-relaxed text-muted-foreground sm:text-lg"
          >
            {t('roadmap.hero.subtitle')}
          </motion.p>
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.3, ease }}
            className="mt-8 flex flex-wrap gap-4"
          >
            <GlowButton variant="primary" to="/register">
              {t('roadmap.cta.ctaPrimary')}
            </GlowButton>
            <GlowButton
              variant="outline"
              href="https://github.com/btachinardi/lemon-todo"
              target="_blank"
              rel="noopener noreferrer"
            >
              <GithubIcon className="size-4" />
              {t('roadmap.cta.ctaGithub')}
            </GlowButton>
          </motion.div>
        </div>

        {/* Roadmap timeline visual */}
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
              <span className="ml-2 text-xs text-muted-foreground">roadmap</span>
            </div>

            {/* Completed checkpoints */}
            <div className="space-y-1.5">
              {checkpoints.map((cp, i) => (
                <div key={i} className="flex items-center gap-3">
                  <span className="flex size-5 items-center justify-center text-xs text-success-foreground">
                    &#10003;
                  </span>
                  <span className="text-xs text-muted-foreground/70">
                    CP{i + 1} &middot; {cp}
                  </span>
                </div>
              ))}
            </div>

            {/* Divider — current position */}
            <div className="my-3 flex items-center gap-2">
              <div className="h-px flex-1 bg-primary/30" />
              <span className="text-[10px] font-bold tracking-wider text-primary">YOU ARE HERE</span>
              <div className="h-px flex-1 bg-primary/30" />
            </div>

            {/* Upcoming tiers */}
            <div className="space-y-1.5">
              {upcomingTiers.map((tier, i) => (
                <div key={i} className="flex items-center gap-3">
                  <span className="flex size-5 items-center justify-center">
                    <span
                      className={`size-2 rounded-full ${i < 2 ? 'bg-primary animate-pulse' : 'bg-border'}`}
                    />
                  </span>
                  <span
                    className={`text-xs ${i < 2 ? 'font-semibold text-primary' : 'text-muted-foreground/50'}`}
                  >
                    Tier {i + 1} &middot; {tier}
                  </span>
                </div>
              ))}
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
}
