import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/ui/dialog';
import { Button } from '@/ui/button';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/ui/select';
import { Input } from '@/ui/input';
import { Textarea } from '@/ui/textarea';
import { Label } from '@/ui/label';
import { AlertTriangleIcon } from 'lucide-react';
import { PROTECTED_DATA_REVEAL_REASONS, type ProtectedDataRevealReason, type RevealProtectedDataRequest } from '../../types/admin.types';

interface ProtectedDataRevealDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onReveal: (request: RevealProtectedDataRequest) => void;
  isPending: boolean;
  error?: Error | null;
}

/** Break-the-glass dialog for revealing protected data. Collects justification reason, optional comments, and password. */
export function ProtectedDataRevealDialog({
  open,
  onOpenChange,
  onReveal,
  isPending,
  error,
}: ProtectedDataRevealDialogProps) {
  const { t } = useTranslation();
  const [reason, setReason] = useState<ProtectedDataRevealReason | ''>('');
  const [reasonDetails, setReasonDetails] = useState('');
  const [comments, setComments] = useState('');
  const [password, setPassword] = useState('');

  const resetForm = () => {
    setReason('');
    setReasonDetails('');
    setComments('');
    setPassword('');
  };

  const handleOpenChange = (newOpen: boolean) => {
    if (!newOpen) resetForm();
    onOpenChange(newOpen);
  };

  const isOther = reason === 'Other';
  const isReasonValid = reason !== '' && (!isOther || reasonDetails.trim().length > 0);
  const isFormValid = isReasonValid && password.length > 0;

  const isPasswordError = error?.message?.includes('401') || error?.message?.includes('unauthorized');

  const handleSubmit = () => {
    if (!isFormValid || !reason) return;
    onReveal({
      reason,
      reasonDetails: isOther ? reasonDetails.trim() : undefined,
      comments: comments.trim() || undefined,
      password,
    });
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangleIcon className="size-5 text-amber-500" />
            {t('admin.protectedDataRevealDialog.title')}
          </DialogTitle>
          <DialogDescription>
            {t('admin.protectedDataRevealDialog.description')}
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4 py-2">
          {/* Reason */}
          <div className="space-y-2">
            <Label htmlFor="protected-data-reason">{t('admin.protectedDataRevealDialog.reason')}</Label>
            <Select value={reason} onValueChange={(v) => setReason(v as ProtectedDataRevealReason)}>
              <SelectTrigger id="protected-data-reason">
                <SelectValue placeholder={t('admin.protectedDataRevealDialog.reasonPlaceholder')} />
              </SelectTrigger>
              <SelectContent>
                {PROTECTED_DATA_REVEAL_REASONS.map((r) => (
                  <SelectItem key={r} value={r}>
                    {t(`admin.protectedDataRevealDialog.reasons.${r}`)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Reason Details (required when Other) */}
          {isOther && (
            <div className="space-y-2">
              <Label htmlFor="protected-data-reason-details">{t('admin.protectedDataRevealDialog.reasonDetails')}</Label>
              <Textarea
                id="protected-data-reason-details"
                value={reasonDetails}
                onChange={(e) => setReasonDetails(e.target.value)}
                placeholder={t('admin.protectedDataRevealDialog.reasonDetailsPlaceholder')}
                rows={2}
              />
              {isOther && reasonDetails.trim().length === 0 && (
                <p className="text-xs text-destructive">
                  {t('admin.protectedDataRevealDialog.reasonDetailsRequired')}
                </p>
              )}
            </div>
          )}

          {/* Comments (always optional) */}
          <div className="space-y-2">
            <Label htmlFor="protected-data-comments">{t('admin.protectedDataRevealDialog.comments')}</Label>
            <Textarea
              id="protected-data-comments"
              value={comments}
              onChange={(e) => setComments(e.target.value)}
              placeholder={t('admin.protectedDataRevealDialog.commentsPlaceholder')}
              rows={2}
            />
          </div>

          {/* Password re-authentication */}
          <div className="space-y-2">
            <Label htmlFor="protected-data-password">{t('admin.protectedDataRevealDialog.password')}</Label>
            <Input
              id="protected-data-password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder={t('admin.protectedDataRevealDialog.passwordPlaceholder')}
              onKeyDown={(e) => {
                if (e.key === 'Enter' && isFormValid && !isPending) handleSubmit();
              }}
            />
            {isPasswordError && (
              <p className="text-xs text-destructive">
                {t('admin.protectedDataRevealDialog.passwordError')}
              </p>
            )}
          </div>

          {/* Audit warning */}
          <div className="flex items-start gap-2 rounded-md border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950">
            <AlertTriangleIcon className="mt-0.5 size-4 shrink-0 text-amber-600 dark:text-amber-400" />
            <p className="text-xs text-amber-700 dark:text-amber-300">
              {t('admin.protectedDataRevealDialog.auditWarning')}
            </p>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            {t('common.cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleSubmit}
            disabled={!isFormValid || isPending}
          >
            {isPending ? t('admin.protectedDataRevealDialog.submitting') : t('admin.protectedDataRevealDialog.submit')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
