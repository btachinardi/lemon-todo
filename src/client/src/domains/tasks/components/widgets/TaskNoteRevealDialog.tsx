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
import { LockIcon, EyeOffIcon } from 'lucide-react';
import { Progress } from '@/ui/progress';

interface TaskNoteRevealDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onReveal: (password: string) => void;
  isPending: boolean;
  error?: Error | null;
  revealedNote?: string | null;
}

const REVEAL_DURATION_SECONDS = 30;

/** Dialog for viewing an encrypted sensitive note. Requires password re-authentication and auto-hides after 30 seconds. */
export function TaskNoteRevealDialog({
  open,
  onOpenChange,
  onReveal,
  isPending,
  error,
  revealedNote,
}: TaskNoteRevealDialogProps) {
  const { t } = useTranslation();
  const [password, setPassword] = useState('');
  const [secondsLeft, setSecondsLeft] = useState(REVEAL_DURATION_SECONDS);

  const resetForm = useCallback(() => {
    setPassword('');
    setSecondsLeft(REVEAL_DURATION_SECONDS);
  }, []);

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) resetForm();
    onOpenChange(newOpen);
  };

  // Countdown timer when note is revealed
  useEffect(() => {
    if (!revealedNote) return;
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
  }, [revealedNote]);

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
            <LockIcon className="size-5 text-amber-500" />
            {t('tasks.noteRevealDialog.title')}
          </DialogTitle>
          <DialogDescription>
            {revealedNote
              ? t('tasks.noteRevealDialog.revealedDescription')
              : t('tasks.noteRevealDialog.description')}
          </DialogDescription>
        </DialogHeader>

        {revealedNote ? (
          <div className="space-y-3 py-2">
            {/* Revealed content */}
            <div className="rounded-md border border-amber-200 bg-amber-50 p-4 dark:border-amber-800 dark:bg-amber-950">
              <p className="whitespace-pre-wrap text-sm">{revealedNote}</p>
            </div>

            {/* Countdown */}
            <div className="space-y-1">
              <div className="flex items-center justify-between text-xs text-muted-foreground">
                <span>{t('tasks.noteRevealDialog.autoHide')}</span>
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
                {t('tasks.noteRevealDialog.hide')}
              </Button>
            </DialogFooter>
          </div>
        ) : (
          <div className="space-y-4 py-2">
            {/* Password re-authentication */}
            <div className="space-y-2">
              <Label htmlFor="task-note-password">
                {t('tasks.noteRevealDialog.password')}
              </Label>
              <Input
                id="task-note-password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                placeholder={t('tasks.noteRevealDialog.passwordPlaceholder')}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && password && !isPending)
                    handleSubmit();
                }}
              />
              {isPasswordError && (
                <p className="text-xs text-destructive">
                  {t('tasks.noteRevealDialog.passwordError')}
                </p>
              )}
              {isGenericError && (
                <p className="text-xs text-destructive">
                  {t('tasks.noteRevealDialog.genericError')}
                </p>
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
                  ? t('tasks.noteRevealDialog.submitting')
                  : t('tasks.noteRevealDialog.submit')}
              </Button>
            </DialogFooter>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
