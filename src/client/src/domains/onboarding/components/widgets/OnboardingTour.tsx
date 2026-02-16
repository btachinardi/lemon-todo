import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Button } from '@/ui/button';
import { OnboardingTooltip } from '../atoms/OnboardingTooltip';
import { CelebrationAnimation } from '../atoms/CelebrationAnimation';
import { useOnboardingStatus, useCompleteOnboarding } from '../../hooks/use-onboarding';

type TourStep = 1 | 2 | 3;

/**
 * Three-step onboarding tour that guides new users through core features.
 *
 * Step 1: "Create your first task" — highlights QuickAddForm
 * Step 2: "Complete it by clicking the checkbox" — highlights first task card
 * Step 3: "Explore your board!" — highlights the board columns
 *
 * Auto-advances from step 1 to step 2 when a task appears, and from step 2
 * to step 3 when any task is completed. Renders nothing for users who have
 * already completed onboarding.
 */
export function OnboardingTour() {
  const { t } = useTranslation();
  const { data: status, isLoading } = useOnboardingStatus();
  const completeOnboarding = useCompleteOnboarding();

  const [step, setStep] = useState<TourStep>(1);
  const [showCelebration, setShowCelebration] = useState(false);
  const [dismissed, setDismissed] = useState(false);

  // Auto-advance: step 1 → 2 when a task card appears
  useEffect(() => {
    if (step !== 1) return;

    const observer = new MutationObserver(() => {
      const taskCard = document.querySelector('[data-onboarding="task-card"]');
      if (taskCard) setStep(2);
    });

    // Check immediately
    const existing = document.querySelector('[data-onboarding="task-card"]');
    if (existing) {
      setStep(2);
      return;
    }

    observer.observe(document.body, { childList: true, subtree: true });
    return () => observer.disconnect();
  }, [step]);

  // Auto-advance: step 2 → 3 when a done task appears
  useEffect(() => {
    if (step !== 2) return;

    const observer = new MutationObserver(() => {
      const doneTask = document.querySelector('[data-onboarding="task-done"]');
      if (doneTask) setStep(3);
    });

    const existing = document.querySelector('[data-onboarding="task-done"]');
    if (existing) {
      setStep(3);
      return;
    }

    observer.observe(document.body, { childList: true, subtree: true });
    return () => observer.disconnect();
  }, [step]);

  const handleSkip = useCallback(() => {
    setDismissed(true);
    completeOnboarding.mutate();
  }, [completeOnboarding]);

  const handleFinish = useCallback(() => {
    setShowCelebration(true);
  }, []);

  const handleCelebrationComplete = useCallback(() => {
    setShowCelebration(false);
    setDismissed(true);
    completeOnboarding.mutate();
  }, [completeOnboarding]);

  // Don't render while loading, if completed, or if dismissed this session
  if (isLoading || status?.completed || dismissed) return null;

  return (
    <>
      {/* Subtle backdrop overlay to draw attention */}
      <div className="fixed inset-0 z-[55] bg-background/30 pointer-events-none" />

      {/* Step 1: Create your first task */}
      <OnboardingTooltip
        targetSelector="#quick-add-input"
        position="bottom"
        visible={step === 1}
      >
        <p className="text-sm font-semibold">{t('onboarding.step1Title')}</p>
        <p className="mt-1 text-xs text-muted-foreground">{t('onboarding.step1Body')}</p>
        <div className="mt-3 flex items-center justify-between">
          <StepIndicator current={1} total={3} />
          <Button variant="ghost" size="sm" onClick={handleSkip}>
            {t('onboarding.skip')}
          </Button>
        </div>
      </OnboardingTooltip>

      {/* Step 2: Complete the task */}
      <OnboardingTooltip
        targetSelector='[data-onboarding="task-card"]'
        position="bottom"
        visible={step === 2}
      >
        <p className="text-sm font-semibold">{t('onboarding.step2Title')}</p>
        <p className="mt-1 text-xs text-muted-foreground">{t('onboarding.step2Body')}</p>
        <div className="mt-3 flex items-center justify-between">
          <StepIndicator current={2} total={3} />
          <Button variant="ghost" size="sm" onClick={handleSkip}>
            {t('onboarding.skip')}
          </Button>
        </div>
      </OnboardingTooltip>

      {/* Step 3: Explore your board */}
      <OnboardingTooltip
        targetSelector='[data-onboarding="board-columns"]'
        position="top"
        visible={step === 3}
      >
        <p className="text-sm font-semibold">{t('onboarding.step3Title')}</p>
        <p className="mt-1 text-xs text-muted-foreground">{t('onboarding.step3Body')}</p>
        <div className="mt-3 flex items-center justify-between">
          <StepIndicator current={3} total={3} />
          <Button size="sm" onClick={handleFinish}>
            {t('onboarding.finish')}
          </Button>
        </div>
      </OnboardingTooltip>

      {/* Celebration overlay on tour completion */}
      <CelebrationAnimation visible={showCelebration} onComplete={handleCelebrationComplete} />
    </>
  );
}

/** Dot indicator showing current step progress. */
function StepIndicator({ current, total }: { current: number; total: number }) {
  return (
    <div className="flex gap-1.5" aria-label={`Step ${current} of ${total}`}>
      {Array.from({ length: total }, (_, i) => (
        <div
          key={i}
          className={`size-1.5 rounded-full transition-colors ${
            i < current ? 'bg-primary' : 'bg-muted-foreground/30'
          }`}
        />
      ))}
    </div>
  );
}
