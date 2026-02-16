import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { GithubIcon } from 'lucide-react';
import { ThemeToggle } from '@/domains/tasks/components/atoms/ThemeToggle';
import { LanguageSwitcher } from '@/domains/tasks/components/atoms/LanguageSwitcher';
import { useThemeStore } from '@/stores/use-theme-store';

interface LandingLayoutProps {
  children: ReactNode;
}

/** Minimal landing page layout with transparent header and branded footer. */
export function LandingLayout({ children }: LandingLayoutProps) {
  const { t } = useTranslation();
  const theme = useThemeStore((s) => s.theme);

  return (
    <div className="flex min-h-screen flex-col">
      <header className="fixed top-0 z-50 w-full border-b border-border/20 bg-background/80 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4 sm:h-16 sm:px-6">
          <Link to="/" className="flex items-center gap-1.5">
            <img src="/lemondo-icon.png" alt="" className="size-7 sm:size-8" />
            <span className="font-[var(--font-brand)] text-lg font-black tracking-tight sm:text-xl">
              <span className="text-foreground">{t('brand.lemon')}</span>
              <span className="text-lemon">{t('brand.do')}</span>
            </span>
          </Link>
          <nav className="flex items-center gap-1 sm:gap-2">
            <a
              href="https://github.com/btachinardi/lemon-todo"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center justify-center rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
              aria-label="GitHub"
            >
              <GithubIcon className="size-4" />
            </a>
            <Link
              to="/story"
              className="rounded-md px-3 py-1.5 text-sm font-semibold text-muted-foreground transition-colors hover:text-foreground"
            >
              {t('story.nav')}
            </Link>
            <LanguageSwitcher />
            <ThemeToggle
              theme={theme}
              onToggle={() => {
                const themes: Array<typeof theme> = ['light', 'dark', 'system'];
                const idx = themes.indexOf(theme);
                const next = themes[(idx + 1) % themes.length];
                useThemeStore.getState().setTheme(next);
              }}
            />
            <Link
              to="/login"
              className="rounded-md px-3 py-1.5 text-sm font-semibold text-muted-foreground transition-colors hover:text-foreground"
            >
              {t('landing.nav.login')}
            </Link>
            <Link
              to="/register"
              className="rounded-lg bg-primary px-3.5 py-1.5 text-sm font-bold text-primary-foreground transition-all hover:shadow-[0_0_16px_rgba(220,255,2,0.3)]"
            >
              {t('landing.nav.getStarted')}
            </Link>
          </nav>
        </div>
      </header>

      <main className="flex-1">{children}</main>

      <footer className="border-t border-border/20 px-4 py-12 sm:px-6">
        <div className="mx-auto flex max-w-7xl flex-col items-center gap-6 text-center">
          <div className="flex items-center gap-1.5">
            <img src="/lemondo-icon.png" alt="" className="size-6" />
            <span className="font-[var(--font-brand)] text-lg font-black tracking-tight">
              <span className="text-foreground">{t('brand.lemon')}</span>
              <span className="text-lemon">{t('brand.do')}</span>
            </span>
          </div>
          <p className="text-sm text-muted-foreground">{t('landing.footer.tagline')}</p>
          <nav className="flex flex-wrap justify-center gap-x-6 gap-y-2 text-sm text-muted-foreground">
            <a href="#features" className="hover:text-foreground">{t('landing.footer.features')}</a>
            <a href="#security" className="hover:text-foreground">{t('landing.footer.security')}</a>
            <Link to="/story" className="hover:text-foreground">{t('landing.footer.story')}</Link>
            <a
              href="https://github.com/btachinardi/lemon-todo"
              target="_blank"
              rel="noopener noreferrer"
              className="hover:text-foreground"
            >
              GitHub
            </a>
            <Link to="/login" className="hover:text-foreground">{t('landing.nav.login')}</Link>
            <Link to="/register" className="hover:text-foreground">{t('landing.nav.getStarted')}</Link>
          </nav>
          <p className="text-xs text-muted-foreground/50">
            &copy; {new Date().getFullYear()} Lemon.DO &middot; v{__APP_VERSION__}
          </p>
        </div>
      </footer>
    </div>
  );
}
