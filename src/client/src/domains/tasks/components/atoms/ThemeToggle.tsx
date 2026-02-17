import { useTranslation } from 'react-i18next';
import { SunIcon, MoonIcon, MonitorIcon } from 'lucide-react';
import { Button } from '@/ui/button';

type Theme = 'light' | 'dark' | 'system';

const options: { value: Theme; icon: typeof SunIcon; labelKey: string }[] = [
  { value: 'light', icon: SunIcon, labelKey: 'theme.light' },
  { value: 'dark', icon: MoonIcon, labelKey: 'theme.dark' },
  { value: 'system', icon: MonitorIcon, labelKey: 'theme.system' },
];

interface ThemeToggleProps {
  theme: Theme;
  onToggle: () => void;
  /** When true, renders a visible text label next to the icon. */
  showLabel?: boolean;
}

/**
 * Compact theme toggle button. Displays the current theme icon and cycles on click.
 * Receives theme state via props — parent component manages theme store.
 */
export function ThemeToggle({ theme, onToggle, showLabel }: ThemeToggleProps) {
  const { t } = useTranslation();
  const currentIndex = options.findIndex((o) => o.value === theme);
  const next = options[(currentIndex + 1) % options.length];
  const current = options[currentIndex] ?? options[1]; // fallback to dark
  const Icon = current.icon;

  return (
    <Button
      variant="ghost"
      size={showLabel ? 'sm' : 'icon'}
      className={showLabel ? 'justify-start gap-2 px-3 py-2' : 'size-9 sm:size-8'}
      onClick={onToggle}
      aria-label={t(current.labelKey)}
      title={t('theme.toggle', { label: t(current.labelKey), next: t(next.labelKey) })}
    >
      <Icon className="size-4" />
      {showLabel && <span className="text-base">{t(current.labelKey)}</span>}
    </Button>
  );
}
