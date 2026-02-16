import { useMemo } from 'react';
import { useTranslation } from 'react-i18next';

/** Individual requirement check result. */
export interface PasswordCheck {
  key: string;
  passed: boolean;
  required: boolean;
}

/** Strength levels matching backend ASP.NET Identity password policy. */
export type StrengthLevel = 'tooWeak' | 'weak' | 'fair' | 'strong' | 'veryStrong';

export interface PasswordStrengthResult {
  score: number; // 0-6
  level: StrengthLevel;
  checks: PasswordCheck[];
}

/**
 * Evaluates password strength against backend requirements:
 * - Min 8 chars (required)
 * - Uppercase letter (required)
 * - Lowercase letter (required)
 * - Digit (required)
 * - Special char (bonus)
 * - 12+ chars (bonus)
 */
// eslint-disable-next-line react-refresh/only-export-components
export function evaluatePasswordStrength(password: string): PasswordStrengthResult {
  const checks: PasswordCheck[] = [
    { key: 'minLength', passed: password.length >= 8, required: true },
    { key: 'hasUppercase', passed: /[A-Z]/.test(password), required: true },
    { key: 'hasLowercase', passed: /[a-z]/.test(password), required: true },
    { key: 'hasDigit', passed: /\d/.test(password), required: true },
    { key: 'hasSpecial', passed: /[^A-Za-z0-9]/.test(password), required: false },
    { key: 'longPassword', passed: password.length >= 12, required: false },
  ];

  const score = checks.filter((c) => c.passed).length;
  const requiredMet = checks.filter((c) => c.required).every((c) => c.passed);

  let level: StrengthLevel;
  if (!requiredMet) {
    level = score <= 1 ? 'tooWeak' : score <= 2 ? 'weak' : 'fair';
  } else if (score <= 4) {
    level = 'fair';
  } else if (score === 5) {
    level = 'strong';
  } else {
    level = 'veryStrong';
  }

  return { score, level, checks };
}

const LEVEL_COLORS: Record<StrengthLevel, string> = {
  tooWeak: 'bg-destructive',
  weak: 'bg-[oklch(0.70_0.18_50)]', // orange
  fair: 'bg-[oklch(0.80_0.16_80)]', // amber
  strong: 'bg-[oklch(0.75_0.18_145)]', // green
  veryStrong: 'bg-primary', // lime
};

const LEVEL_TEXT_COLORS: Record<StrengthLevel, string> = {
  tooWeak: 'text-destructive',
  weak: 'text-[oklch(0.70_0.18_50)]',
  fair: 'text-[oklch(0.80_0.16_80)]',
  strong: 'text-[oklch(0.75_0.18_145)]',
  veryStrong: 'text-primary',
};

interface PasswordStrengthMeterProps {
  password: string;
}

/** Animated password strength meter with individual requirement checks. */
export function PasswordStrengthMeter({ password }: PasswordStrengthMeterProps) {
  const { t } = useTranslation();
  const { score, level, checks } = useMemo(() => evaluatePasswordStrength(password), [password]);

  if (!password) return null;

  const percentage = (score / 6) * 100;
  const requiredChecks = checks.filter((c) => c.required);
  const bonusChecks = checks.filter((c) => !c.required);

  return (
    <div
      className="animate-fade-in space-y-3"
      role="status"
      aria-label={t('auth.passwordStrength.label')}
      aria-live="polite"
    >
      {/* Strength bar */}
      <div className="space-y-1.5">
        <div className="flex items-center justify-between">
          <span className="text-xs font-medium text-muted-foreground">
            {t('auth.passwordStrength.label')}
          </span>
          <span className={`text-xs font-semibold ${LEVEL_TEXT_COLORS[level]}`}>
            {t(`auth.passwordStrength.${level}`)}
          </span>
        </div>
        <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
          <div
            className={`h-full rounded-full transition-all duration-500 ease-out ${LEVEL_COLORS[level]}`}
            style={{ width: `${percentage}%` }}
          />
        </div>
      </div>

      {/* Requirement checklist */}
      <div className="space-y-2">
        <div className="space-y-1">
          <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground/70">
            {t('auth.passwordStrength.requirements')}
          </span>
          <ul className="space-y-0.5">
            {requiredChecks.map((check) => (
              <RequirementItem
                key={check.key}
                label={t(`auth.passwordStrength.${check.key}`)}
                passed={check.passed}
              />
            ))}
          </ul>
        </div>

        <div className="space-y-1">
          <span className="text-[11px] font-medium uppercase tracking-wider text-muted-foreground/70">
            {t('auth.passwordStrength.bonus')}
          </span>
          <ul className="space-y-0.5">
            {bonusChecks.map((check) => (
              <RequirementItem
                key={check.key}
                label={t(`auth.passwordStrength.${check.key}`)}
                passed={check.passed}
              />
            ))}
          </ul>
        </div>
      </div>
    </div>
  );
}

function RequirementItem({ label, passed }: { label: string; passed: boolean }) {
  return (
    <li className="flex items-center gap-2 text-xs">
      <span
        className={`flex size-4 shrink-0 items-center justify-center rounded-full transition-all duration-300 ${
          passed
            ? 'bg-primary/20 text-primary scale-100'
            : 'bg-muted text-muted-foreground scale-90'
        }`}
      >
        {passed ? (
          <svg
            className="size-2.5 animate-[draw-check_0.3s_ease-out_forwards]"
            viewBox="0 0 12 12"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          >
            <path
              d="M2 6l3 3 5-5"
              strokeDasharray="16"
              strokeDashoffset="0"
            />
          </svg>
        ) : (
          <span className="size-1 rounded-full bg-current opacity-40" />
        )}
      </span>
      <span
        className={`transition-colors duration-200 ${
          passed ? 'text-foreground' : 'text-muted-foreground'
        }`}
      >
        {label}
      </span>
    </li>
  );
}
