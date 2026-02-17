import { useState, useEffect, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { ApiRequestError } from '@/lib/api-client';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/ui/dialog';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { Label } from '@/ui/label';
import { ShieldCheckIcon, EyeOffIcon, FlaskConicalIcon } from 'lucide-react';
import { Progress } from '@/ui/progress';

interface SelfRevealDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onReveal: (password: string) => void;
  isPending: boolean;
  error?: Error | null;
  revealedEmail?: string | null;
  revealedDisplayName?: string | null;
  /** Dev-only: when provided, shows an auto-fill button that populates the password field with this value. */
  devPassword?: string | null;
}

const REVEAL_DURATION_SECONDS = 30;

/** Dialog for a user to reveal their own redacted profile data. Requires password re-authentication and auto-hides after 30 seconds. */
export function SelfRevealDialog({
  open,
  onOpenChange,
  onReveal,
  isPending,
  error,
  revealedEmail,
  revealedDisplayName,
  devPassword,
}: SelfRevealDialogProps) {
  const { t } = useTranslation();
  const [password, setPassword] = useState('');
  const [secondsLeft, setSecondsLeft] = useState(REVEAL_DURATION_SECONDS);

  const isRevealed = !!revealedEmail && !!revealedDisplayName;

  const resetForm = useCallback(() => {
    setPassword('');
    setSecondsLeft(REVEAL_DURATION_SECONDS);
  }, []);

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) resetForm();
    onOpenChange(newOpen);
  };

  // Countdown timer when data is revealed
  useEffect(() => {
    if (!isRevealed) return;
    setSecondsLeft(REVEAL_DURATION_SECONDS);
    const interval = setInterval(() => {
      setSecondsLeft((prev) => {
        if (prev <= 1) {
          clearInterval(interval);
          handleOpenChange(false);
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
    return () => clearInterval(interval);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isRevealed]);

  const isPasswordError = error instanceof ApiRequestError && error.status === 401;
  const isGenericError = !!error && !isPasswordError;

  const handleSubmit = () => {
    if (!password || isPending) return;
    onReveal(password);
  };

  const progressPercent = (secondsLeft / REVEAL_DURATION_SECONDS) * 100;

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShieldCheckIcon className="size-5 text-emerald-500" />
            {t('auth.selfReveal.title')}
          </DialogTitle>
          <DialogDescription>
            {isRevealed
              ? t('auth.selfReveal.revealedDescription')
              : t('auth.selfReveal.description')}
          </DialogDescription>
        </DialogHeader>

        {isRevealed ? (
          <div className="space-y-3 py-2">
            {/* Revealed content */}
            <div className="rounded-md border border-emerald-200 bg-emerald-50 p-4 dark:border-emerald-800 dark:bg-emerald-950">
              <div className="space-y-2">
                <div>
                  <p className="text-xs font-medium text-muted-foreground">{t('auth.selfReveal.emailLabel')}</p>
                  <p className="text-sm font-medium">{revealedEmail}</p>
                </div>
                <div>
                  <p className="text-xs font-medium text-muted-foreground">{t('auth.selfReveal.nameLabel')}</p>
                  <p className="text-sm font-medium">{revealedDisplayName}</p>
                </div>
              </div>
            </div>

            {/* Countdown */}
            <div className="space-y-1">
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>{t('auth.selfReveal.autoHide')}</span>
                <span className="font-mono">{secondsLeft}s</span>
              </div>
              <Progress value={progressPercent} className="h-1" />
            </div>

            <DialogFooter>
              <Button
                variant="outline"
                onClick={() => handleOpenChange(false)}
                className="gap-2"
              >
                <EyeOffIcon className="size-4" />
                {t('auth.selfReveal.hide')}
              </Button>
            </DialogFooter>
          </div>
        ) : (
          <div className="space-y-4 py-2">
            {/* Password re-authentication */}
            <div className="space-y-2">
              <Label htmlFor="self-reveal-password">
                {t('auth.selfReveal.password')}
              </Label>
              <Input
                id="self-reveal-password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={t('auth.selfReveal.passwordPlaceholder')}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && password && !isPending)
                    handleSubmit();
                }}
              />
              {isPasswordError && (
                <p className="text-xs text-destructive">
                  {t('auth.selfReveal.passwordError')}
                </p>
              )}
              {isGenericError && (
                <p className="text-xs text-destructive">
                  {t('auth.selfReveal.genericError')}
                </p>
              )}
              {devPassword && (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="gap-1.5 border-dashed border-purple-300 text-purple-600 hover:bg-purple-50 dark:border-purple-700 dark:text-purple-400 dark:hover:bg-purple-950"
                  onClick={() => setPassword(devPassword)}
                >
                  <FlaskConicalIcon className="size-3" />
                  {t('common.devAutoFill')}
                </Button>
              )}
            </div>

            <DialogFooter>
              <Button variant="outline" onClick={() => handleOpenChange(false)}>
                {t('common.cancel')}
              </Button>
              <Button
                onClick={handleSubmit}
                disabled={!password || isPending}
              >
                {isPending
                  ? t('auth.selfReveal.submitting')
                  : t('auth.selfReveal.submit')}
              </Button>
            </DialogFooter>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
