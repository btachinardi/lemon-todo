import { useEffect, useState } from 'react';
import { CheckCircle2Icon } from 'lucide-react';

interface CelebrationAnimationProps {
  visible: boolean;
  onComplete: () => void;
}

/**
 * Shows a celebratory checkmark burst animation, then auto-dismisses.
 */
export function CelebrationAnimation({ visible, onComplete }: CelebrationAnimationProps) {
  const [show, setShow] = useState(false);

  useEffect(() => {
    if (!visible) return;
    // Use queueMicrotask to avoid synchronous setState in effect body
    queueMicrotask(() => setShow(true));
    const timer = setTimeout(() => {
      setShow(false);
      onComplete();
    }, 2000);
    return () => clearTimeout(timer);
  }, [visible, onComplete]);

  if (!show) return null;

  return (
    <div className="fixed inset-0 z-[70] flex items-center justify-center bg-background/50 animate-in fade-in-0">
      <div className="flex flex-col items-center gap-3 animate-in zoom-in-50">
        <CheckCircle2Icon className="size-20 text-green-500 animate-bounce" />
        <p className="text-xl font-bold text-foreground">All set!</p>
      </div>
    </div>
  );
}
