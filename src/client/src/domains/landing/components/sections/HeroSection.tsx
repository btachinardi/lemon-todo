import { useTranslation } from 'react-i18next';
import { motion } from 'motion/react';
import { GithubIcon } from 'lucide-react';
import { GlowButton } from '../atoms/GlowButton';

const ease = [0.25, 0.46, 0.45, 0.94] as const;

/** Bold hero section with headline, CTAs, and stylized kanban preview. */
export function HeroSection() {
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
            {t('landing.hero.titleLine1')}{' '}
            {t('landing.hero.titleLine2')}{' '}
            <span className="text-highlight glow-text">{t('landing.hero.titleHighlight')}</span>
          </motion.h1>
          <motion.p
            initial={{ opacity: 0, y: 24 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.7, delay: 0.15, ease }}
            className="mt-6 max-w-xl text-base leading-relaxed text-muted-foreground sm:text-lg"
          >
            {t('landing.hero.subtitle')}
          </motion.p>
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.6, delay: 0.3, ease }}
            className="mt-8 flex flex-wrap gap-4"
          >
            <GlowButton variant="primary" to="/register">
              {t('landing.hero.ctaPrimary')}
            </GlowButton>
            <GlowButton
              variant="outline"
              href="https://github.com/btachinardi/lemon-todo"
              target="_blank"
              rel="noopener noreferrer"
            >
              <GithubIcon className="size-4" />
              {t('landing.hero.ctaGithub')}
            </GlowButton>
          </motion.div>
        </div>

        {/* Stylized kanban preview */}
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
              <span className="ml-2 text-xs text-muted-foreground">Lemon.DO</span>
            </div>
            <div className="grid grid-cols-3 gap-3">
              {(['To Do', 'In Progress', 'Done'] as const).map((col) => (
                <div key={col} className="rounded-lg bg-background/60 p-3">
                  <h4 className="mb-3 text-xs font-bold uppercase tracking-wider text-muted-foreground">
                    {col}
                  </h4>
                  <div className="space-y-2">
                    {col === 'To Do' && (
                      <>
                        <MockCard label="Setup CI/CD pipeline" priority="high" />
                        <MockCard label="Design landing page" priority="medium" />
                        <MockCard label="Add unit tests" priority="low" />
                      </>
                    )}
                    {col === 'In Progress' && (
                      <>
                        <MockCard label="Implement auth flow" priority="critical" />
                        <MockCard label="Build kanban board" priority="high" />
                      </>
                    )}
                    {col === 'Done' && (
                      <>
                        <MockCard label="Project scaffolding" priority="medium" done />
                        <MockCard label="Database schema" priority="high" done />
                      </>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </motion.div>
      </div>
    </section>
  );
}

function MockCard({
  label,
  priority,
  done,
}: {
  label: string;
  priority: 'low' | 'medium' | 'high' | 'critical';
  done?: boolean;
}) {
  const dotColors = {
    low: 'bg-priority-low-foreground',
    medium: 'bg-priority-medium-foreground',
    high: 'bg-priority-high-foreground',
    critical: 'bg-priority-critical-foreground',
  };

  return (
    <div className="rounded-md border border-border/30 bg-card/80 px-3 py-2">
      <div className="flex items-center gap-2">
        <span className={`size-1.5 rounded-full ${dotColors[priority]}`} />
        <span className={`text-xs ${done ? 'text-muted-foreground line-through' : 'text-foreground'}`}>
          {label}
        </span>
      </div>
    </div>
  );
}
