import { useState, type ReactNode } from 'react';
import { NavLink } from 'react-router';
import { useTranslation } from 'react-i18next';
import { FlaskConicalIcon, KanbanIcon, ListIcon, MenuIcon, ShieldIcon } from 'lucide-react';
import { Toaster } from 'sonner';
import { cn } from '@/lib/utils';
import { Button } from '@/ui/button';
import { Popover, PopoverContent, PopoverTrigger } from '@/ui/popover';
import { Sheet, SheetContent, SheetHeader, SheetTitle } from '@/ui/sheet';
import { UserMenu } from '@/domains/auth/components/UserMenu';
import { DevAccountSwitcher } from '@/domains/auth/components/DevAccountSwitcher';
import { ThemeToggle } from '@/domains/tasks/components/atoms/ThemeToggle';
import { LanguageSwitcher } from '@/domains/tasks/components/atoms/LanguageSwitcher';
import { useThemeStore, resolveTheme } from '@/stores/use-theme-store';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';
import { NotificationDropdown } from '@/domains/notifications/components/widgets/NotificationDropdown';
import { useOnboardingStatus } from '@/domains/onboarding/hooks/use-onboarding';
import { OnboardingTour } from '@/domains/onboarding/components/widgets/OnboardingTour';
import { PWAInstallPrompt } from '@/ui/feedback/PWAInstallPrompt';
import { SyncIndicator } from '@/ui/feedback/SyncIndicator';

/** Props for {@link DashboardLayout}. */
interface DashboardLayoutProps {
  children: ReactNode;
}

/** App shell with branded header, pill-shaped view switcher, and toast container. */
export function DashboardLayout({ children }: DashboardLayoutProps) {
  const { t } = useTranslation();
  const theme = useThemeStore((s) => s.theme);
  const resolvedTheme = resolveTheme(theme);
  const roles = useAuthStore((s) => s.user?.roles);
  const isAdmin = roles?.some((r) => r === 'Admin' || r === 'SystemAdmin') ?? false;
  const { data: onboardingStatus } = useOnboardingStatus();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

  return (
    <div className="flex min-h-screen flex-col">
      <header className="sticky top-0 z-50 border-b border-border/40 bg-background/90 backdrop-blur-xl">
        <div className="mx-auto flex h-14 max-w-7xl items-center justify-between px-3 sm:h-16 sm:px-6">
          <h1 className="flex items-center gap-1.5">
            <img src="/lemondo-icon.png" alt="" className="size-7 sm:size-8" />
            <span className="font-[var(--font-brand)] text-lg font-black tracking-tight sm:text-xl">
              <span className="text-foreground">{t('brand.lemon')}</span>
              <span className="text-lemon">{t('brand.do')}</span>
            </span>
          </h1>
          <nav
            className="flex items-center gap-1 rounded-lg border-2 border-border/40 bg-secondary/30 p-1"
            aria-label={t('nav.viewSwitcher')}
          >
            <NavLink
              to="/board"
              end
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-semibold transition-all duration-300 sm:px-3.5',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <KanbanIcon className="size-3.5" />
              <span className="hidden sm:inline">{t('nav.board')}</span>
            </NavLink>
            <NavLink
              to="/list"
              className={({ isActive }) =>
                cn(
                  'inline-flex items-center gap-1.5 rounded-md px-2.5 py-1.5 text-sm font-semibold transition-all duration-300 sm:px-3.5',
                  isActive
                    ? 'bg-primary text-primary-foreground shadow-[0_0_16px_rgba(220,255,2,0.3)]'
                    : 'text-muted-foreground hover:text-foreground',
                )
              }
            >
              <ListIcon className="size-3.5" />
              <span className="hidden sm:inline">{t('nav.list')}</span>
            </NavLink>
          </nav>
          {/* Desktop tools — hidden below md */}
          <div className="hidden items-center gap-0.5 md:flex md:gap-1">
            {isAdmin && (
              <NavLink
                to="/admin/users"
                className="inline-flex items-center justify-center gap-1 rounded-md p-2 text-xs text-muted-foreground hover:text-foreground sm:px-2 sm:py-1.5"
              >
                <ShieldIcon className="size-4 sm:size-3" />
                <span className="hidden sm:inline">{t('nav.admin')}</span>
              </NavLink>
            )}
            <NotificationDropdown />
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
            <UserMenu />
          </div>

          {/* Mobile menu trigger — visible below md */}
          <button
            type="button"
            className="inline-flex items-center justify-center rounded-md p-2 text-muted-foreground transition-colors hover:text-foreground md:hidden"
            aria-label="Menu"
            onClick={() => setMobileMenuOpen(true)}
          >
            <MenuIcon className="size-5" />
          </button>

          {/* Mobile menu sheet */}
          <Sheet open={mobileMenuOpen} onOpenChange={setMobileMenuOpen}>
            <SheetContent side="right" className="w-72">
              <SheetHeader>
                <SheetTitle className="sr-only">{t('nav.viewSwitcher')}</SheetTitle>
              </SheetHeader>
              <div className="flex flex-col gap-3 px-4">
                {isAdmin && (
                  <NavLink
                    to="/admin/users"
                    onClick={() => setMobileMenuOpen(false)}
                    className="inline-flex items-center gap-2 rounded-md px-3 py-2 text-sm text-muted-foreground hover:text-foreground"
                  >
                    <ShieldIcon className="size-4" />
                    {t('nav.admin')}
                  </NavLink>
                )}
                <NotificationDropdown />
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
                <UserMenu />
              </div>
            </SheetContent>
          </Sheet>
        </div>
      </header>
      <main className="mx-auto w-full max-w-7xl flex-1">{children}</main>
      {import.meta.env.DEV && (
        <div className="fixed bottom-14 left-3 z-50 sm:bottom-3">
          <Popover>
            <PopoverTrigger asChild>
              <Button
                variant="outline"
                size="sm"
                className="gap-1.5 border-dashed border-amber-500/30 bg-amber-500/5 text-amber-500 shadow-lg hover:bg-amber-500/10 hover:text-amber-400"
              >
                <FlaskConicalIcon className="size-3.5" />
                <span className="text-xs">Dev</span>
              </Button>
            </PopoverTrigger>
            <PopoverContent side="top" align="start" className="w-72 p-3">
              <DevAccountSwitcher />
            </PopoverContent>
          </Popover>
        </div>
      )}
      <footer className="pointer-events-none fixed bottom-2 right-3 flex items-center gap-3">
        <SyncIndicator />
        <span className="text-[10px] text-muted-foreground/40 select-none">
          v{__APP_VERSION__}
        </span>
      </footer>
      {onboardingStatus?.completed && <PWAInstallPrompt />}
      <OnboardingTour />
      <Toaster theme={resolvedTheme} />
    </div>
  );
}
