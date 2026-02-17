import { useState, useEffect, useRef } from 'react';

const DISMISS_DELAY_MS = 2000;

/**
 * Derives a save indicator state from TanStack Query mutation status.
 * Shows 'pending' during mutations, 'success' briefly after completion,
 * then auto-resets to 'idle'. Resets immediately when `taskId` changes
 * so stale status from a previous task never bleeds through.
 */
export function useSaveIndicator(
  isPending: boolean,
  isSuccess: boolean,
  taskId: string | null,
): 'idle' | 'pending' | 'success' {
  const [indicator, setIndicator] = useState<'idle' | 'pending' | 'success'>('idle');
  const timerRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => {
    clearTimeout(timerRef.current);
    if (isPending) {
      setIndicator('pending');
    } else if (isSuccess) {
      setIndicator('success');
      timerRef.current = setTimeout(() => setIndicator('idle'), DISMISS_DELAY_MS);
    }
    return () => clearTimeout(timerRef.current);
  }, [isPending, isSuccess]);

  // Reset when switching tasks so stale 'success' doesn't carry over.
  const prevTaskIdRef = useRef(taskId);
  useEffect(() => {
    if (taskId !== prevTaskIdRef.current) {
      prevTaskIdRef.current = taskId;
      clearTimeout(timerRef.current);
      setIndicator('idle');
    }
  }, [taskId]);

  return indicator;
}
