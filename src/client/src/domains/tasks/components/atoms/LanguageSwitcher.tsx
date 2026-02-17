import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';
import { GlobeIcon } from 'lucide-react';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/ui/dropdown-menu';

const languages = [
  { code: 'en', label: 'English' },
  { code: 'pt-BR', label: 'Português (BR)' },
  { code: 'es', label: 'Español' },
] as const;

interface LanguageSwitcherProps {
  /** When true, renders a visible text label next to the icon. */
  showLabel?: boolean;
}

/**
 * Dropdown to switch between available languages.
 * Self-contained — manages its own i18n state via `useTranslation`.
 */
export function LanguageSwitcher({ showLabel }: LanguageSwitcherProps = {}) {
  const { t, i18n } = useTranslation();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size={showLabel ? 'sm' : 'icon'}
          className={showLabel ? 'justify-start gap-2 px-3 py-2' : 'size-9 sm:size-8'}
        >
          <GlobeIcon className={showLabel ? 'size-4' : 'size-[18px] sm:size-4'} />
          {showLabel ? (
            <span className="text-base">{t('nav.language')}</span>
          ) : (
            <span className="sr-only">
              {i18n.language === 'pt-BR' ? 'Idioma' : 'Language'}
            </span>
          )}
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end">
        {languages.map((lang) => (
          <DropdownMenuItem
            key={lang.code}
            onClick={() => i18n.changeLanguage(lang.code)}
            className={i18n.language === lang.code ? 'font-bold' : ''}
          >
            {lang.label}
          </DropdownMenuItem>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
