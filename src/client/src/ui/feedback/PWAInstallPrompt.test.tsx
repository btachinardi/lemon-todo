import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { PWAInstallPrompt } from './PWAInstallPrompt';

// Ensure localStorage is available (some test environments don't provide it)
const storage: Record<string, string> = {};
vi.hoisted(() => {
  if (typeof globalThis.localStorage === 'undefined' || typeof globalThis.localStorage.clear !== 'function') {
    Object.defineProperty(globalThis, 'localStorage', {
      value: {
        getItem: (key: string) => storage[key] ?? null,
        setItem: (key: string, value: string) => { storage[key] = value; },
        removeItem: (key: string) => { delete storage[key]; },
        clear: () => { for (const k in storage) delete storage[k]; },
        get length() { return Object.keys(storage).length; },
        key: (i: number) => Object.keys(storage)[i] ?? null,
      },
      writable: true,
      configurable: true,
    });
  }
});

const mockPromptInstall = vi.fn().mockResolvedValue(false);
let mockIsInstallAvailable = false;

vi.mock('@/lib/pwa', () => ({
  isInstallAvailable: () => mockIsInstallAvailable,
  onInstallAvailable: vi.fn().mockReturnValue(() => {}),
  promptInstall: (...args: unknown[]) => mockPromptInstall(...args),
}));

vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string) => {
      const translations: Record<string, string> = {
        'pwa.installPrompt': 'Install Lemon.DO for a faster experience',
        'pwa.install': 'Install',
        'common.close': 'Close',
      };
      return translations[key] ?? key;
    },
  }),
}));

const DISMISS_KEY = 'pwa-install-dismissed';

describe('PWAInstallPrompt', () => {
  beforeEach(() => {
    mockIsInstallAvailable = true;
    mockPromptInstall.mockClear();
    localStorage.removeItem(DISMISS_KEY);
  });

  it('should render the banner when install is available and not dismissed', () => {
    render(<PWAInstallPrompt />);
    expect(screen.getByText('Install Lemon.DO for a faster experience')).toBeInTheDocument();
  });

  it('should render nothing when install is not available', () => {
    mockIsInstallAvailable = false;
    const { container } = render(<PWAInstallPrompt />);
    expect(container.firstChild).toBeNull();
  });

  it('should hide the banner when dismiss button is clicked', async () => {
    const user = userEvent.setup();
    render(<PWAInstallPrompt />);

    await user.click(screen.getByRole('button', { name: /close/i }));
    expect(screen.queryByText('Install Lemon.DO for a faster experience')).not.toBeInTheDocument();
  });

  it('should persist dismissal to localStorage', async () => {
    const user = userEvent.setup();
    render(<PWAInstallPrompt />);

    await user.click(screen.getByRole('button', { name: /close/i }));
    expect(localStorage.getItem(DISMISS_KEY)).toBe('1');
  });

  it('should not render when previously dismissed via localStorage', () => {
    localStorage.setItem(DISMISS_KEY, '1');
    const { container } = render(<PWAInstallPrompt />);
    expect(container.firstChild).toBeNull();
  });

  it('should position banner above mobile quick-add bar', () => {
    render(<PWAInstallPrompt />);
    const banner = screen.getByRole('complementary');
    // On mobile (default), bottom-16 clears the fixed quick-add form
    // On sm+, bottom-4 is used (no quick-add bar)
    expect(banner.className).toContain('bottom-16');
    expect(banner.className).toContain('sm:bottom-4');
  });
});
