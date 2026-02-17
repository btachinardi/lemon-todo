import { useState, type ReactNode } from 'react';
import { Link, useLocation } from 'react-router';
import { useTranslation } from 'react-i18next';
import { GithubIcon, MenuIcon, XIcon } from 'lucide-react';
import { ThemeToggle } from '@/domains/tasks/components/atoms/ThemeToggle';
import { LanguageSwitcher } from '@/domains/tasks/components/atoms/LanguageSwitcher';
import { useThemeStore } from '@/stores/use-theme-store';
import { cn } from '@/lib/utils';
import { AssignmentBanner } from '@/domains/landing/components/widgets/AssignmentBanner';

interface LandingLayoutProps {
  children: ReactNode;
}

function isActive(href: string, pathname: string) {
  return href === '/' ? pathname === '/' : pathname === href || pathname.startsWith(`${href}/`);
}

/** Minimal landing page layout with transparent header and branded footer. */
export function LandingLayout({ children }: LandingLayoutProps) {
  const { t } = useTranslation();
  const theme = useThemeStore((s) => s.theme);
  const { pathname } = useLocation();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  const navLinks = [
    { to: '/', label: t('landing.nav.home') },
    { to: '/methodology', label: t('story.nav') },
    { to: '/devops', label: t('devops.nav') },
    { to: '/roadmap', label: t('roadmap.nav') },
  ];

  const themeToggle = (
    <ThemeToggle
      theme={theme}
      onToggle={() => {
        const themes: Array<typeof theme> = ['light', 'dark', 'system'];
        const idx = themes.indexOf(theme);
        const next = themes[(idx + 1) % themes.length];
        useThemeStore.getState().setTheme(next);
      }}
    />
  );

  return (
    <div className="flex min-h-screen flex-col">
      <header className="fixed top-0 z-50 w-full border-b border-border/20 bg-background/80 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-4 sm:h-16 sm:px-6">
          <Link to="/" className="flex items-center gap-1.5">
            <img src="/lemondo-icon.png" alt="" className="size-7 sm:size-8" />
            <span className="font-[var(--font-brand)] text-lg font-black tracking-tight sm:text-xl">
              <span className="text-foreground">{t('brand.lemon')}</span>
              <span className="text-highlight">{t('brand.do')}</span>
            </span>
          </Link>

          {/* Desktop navigation — hidden on mobile */}
          <nav className="hidden items-center gap-1 md:flex md:gap-2" aria-label="Desktop navigation">
            <a
              href="https://github.com/btachinardi/lemon-todo"
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center justify-center rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
              aria-label="GitHub"
            >
              <GithubIcon className="size-4" />
            </a>
            {navLinks.map((link) => (
              <Link
                key={link.to}
                to={link.to}
                className={cn(
                  'rounded-md px-3 py-1.5 text-sm font-semibold transition-colors',
                  isActive(link.to, pathname)
                    ? 'text-foreground'
                    : 'text-muted-foreground hover:text-foreground',
                )}
              >
                {link.label}
                {isActive(link.to, pathname) && (
                  <span className="mt-0.5 block h-0.5 rounded-full bg-primary" />
                )}
              </Link>
            ))}
            <LanguageSwitcher />
            {themeToggle}
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

          {/* Mobile controls — visible only below md breakpoint */}
          <div className="flex items-center gap-1 md:hidden">
            <LanguageSwitcher />
            {themeToggle}
            <button
              onClick={() => setMobileMenuOpen(!mobileMenuOpen)}
              className="inline-flex items-center justify-center rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground"
              aria-label={mobileMenuOpen ? 'Close menu' : 'Open menu'}
            >
              {mobileMenuOpen ? <XIcon className="size-5" /> : <MenuIcon className="size-5" />}
            </button>
          </div>
        </div>

        {/* Mobile navigation overlay */}
        {mobileMenuOpen && (
          <nav
            className="border-t border-border/20 bg-background/95 px-4 pb-6 pt-4 backdrop-blur-xl md:hidden"
            aria-label="Mobile navigation"
          >
            <div className="flex flex-col gap-2">
              {navLinks.map((link) => (
                <Link
                  key={link.to}
                  to={link.to}
                  onClick={() => setMobileMenuOpen(false)}
                  className={cn(
                    'rounded-md px-3 py-2 text-base font-semibold transition-colors',
                    isActive(link.to, pathname)
                      ? 'text-foreground'
                      : 'text-muted-foreground hover:text-foreground',
                  )}
                >
                  {link.label}
                </Link>
              ))}
              <hr className="my-2 border-border/20" />
              <a
                href="https://github.com/btachinardi/lemon-todo"
                target="_blank"
                rel="noopener noreferrer"
                className="flex items-center gap-2 rounded-md px-3 py-2 text-base font-semibold text-muted-foreground transition-colors hover:text-foreground"
              >
                <GithubIcon className="size-4" />
                GitHub
              </a>
              <Link
                to="/login"
                onClick={() => setMobileMenuOpen(false)}
                className="rounded-md px-3 py-2 text-base font-semibold text-muted-foreground transition-colors hover:text-foreground"
              >
                {t('landing.nav.login')}
              </Link>
              <Link
                to="/register"
                onClick={() => setMobileMenuOpen(false)}
                className="mt-2 rounded-lg bg-primary px-3.5 py-2 text-center text-base font-bold text-primary-foreground transition-all hover:shadow-[0_0_16px_rgba(220,255,2,0.3)]"
              >
                {t('landing.nav.getStarted')}
              </Link>
            </div>
          </nav>
        )}
      </header>

      <main className="flex-1">{children}</main>
      <AssignmentBanner />

      <footer className="border-t border-border/20 px-4 py-12 sm:px-6">
        <div className="mx-auto flex max-w-7xl flex-col items-center gap-6 text-center">
          <div className="flex items-center gap-1.5">
            <img src="/lemondo-icon.png" alt="" className="size-6" />
            <span className="font-[var(--font-brand)] text-lg font-black tracking-tight">
              <span className="text-foreground">{t('brand.lemon')}</span>
              <span className="text-highlight">{t('brand.do')}</span>
            </span>
          </div>
          <p className="text-sm text-muted-foreground">{t('landing.footer.tagline')}</p>
          <nav className="flex flex-wrap justify-center gap-x-6 gap-y-2 text-sm text-muted-foreground">
            <a href="#features" className="hover:text-foreground">{t('landing.footer.features')}</a>
            <a href="#security" className="hover:text-foreground">{t('landing.footer.security')}</a>
            <Link to="/methodology" className="hover:text-foreground">{t('landing.footer.story')}</Link>
            <Link to="/devops" className="hover:text-foreground">{t('devops.nav')}</Link>
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
          <p className="text-xs text-muted-foreground">
            {t('landing.footer.disclaimer')}
          </p>
          <p className="text-xs text-muted-foreground">
            &copy; {new Date().getFullYear()} Lemon.DO &middot; v{__APP_VERSION__}
          </p>
        </div>
      </footer>
    </div>
  );
}
