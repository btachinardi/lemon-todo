import { SunIcon, MoonIcon, MonitorIcon } from 'lucide-react';
import { Button } from '@/ui/button';

type Theme = 'light' | 'dark' | 'system';

const options: { value: Theme; icon: typeof SunIcon; label: string }[] = [
  { value: 'light', icon: SunIcon, label: 'Light theme' },
  { value: 'dark', icon: MoonIcon, label: 'Dark theme' },
  { value: 'system', icon: MonitorIcon, label: 'System theme' },
];

interface ThemeToggleProps {
  theme: Theme;
  onToggle: () => void;
}

/** Compact theme toggle button. Displays the current theme icon and cycles on click. */
export function ThemeToggle({ theme, onToggle }: ThemeToggleProps) {
  const currentIndex = options.findIndex((o) => o.value === theme);
  const next = options[(currentIndex + 1) % options.length];
  const current = options[currentIndex] ?? options[1]; // fallback to dark
  const Icon = current.icon;

  return (
    <Button
      variant="ghost"
      size="icon"
      className="size-8"
      onClick={onToggle}
      aria-label={current.label}
      title={`Current: ${current.label}. Click for ${next.label}`}
    >
      <Icon className="size-4" />
    </Button>
  );
}
