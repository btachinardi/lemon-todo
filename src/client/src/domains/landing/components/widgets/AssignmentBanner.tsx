import { useState, useEffect } from 'react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'motion/react';
import { XIcon } from 'lucide-react';

const STORAGE_KEY = 'lemondo-banner-dismissed';

/** Dismissible chat-bubble banner with personal attribution for recruiter review. */
export function AssignmentBanner() {
  const { t } = useTranslation();
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    try {
      if (!localStorage.getItem(STORAGE_KEY)) {
        // Small delay so it appears after page load, not during
        const timer = setTimeout(() => setVisible(true), 800);
        return () => clearTimeout(timer);
      }
    } catch {
      // localStorage unavailable — don't show
    }
  }, []);

  function dismiss() {
    setVisible(false);
    try {
      localStorage.setItem(STORAGE_KEY, '1');
    } catch {
      // Ignore storage errors
    }
  }

  return (
    <AnimatePresence>
      {visible && (
        <motion.div
          initial={{ opacity: 0, y: 40, scale: 0.95 }}
          animate={{ opacity: 1, y: 0, scale: 1 }}
          exit={{ opacity: 0, y: 20, scale: 0.95 }}
          transition={{ duration: 0.4, ease: [0.25, 0.46, 0.45, 0.94] }}
          className="fixed bottom-6 right-6 z-50 w-[min(380px,calc(100vw-3rem))]"
        >
          <div className="relative rounded-2xl border-2 border-primary/30 bg-card/95 p-5 shadow-2xl shadow-primary/10 backdrop-blur-xl">
            {/* Chat tail */}
            <div className="absolute -bottom-2 right-8 size-4 rotate-45 border-b-2 border-r-2 border-primary/30 bg-card/95" />

            {/* Close button */}
            <button
              onClick={dismiss}
              className="absolute right-3 top-3 rounded-full p-1 text-muted-foreground transition-colors hover:text-foreground"
              aria-label={t('assignmentBanner.dismiss')}
            >
              <XIcon className="size-4" />
            </button>

            {/* Header with photo */}
            <div className="mb-3 flex items-center gap-3">
              <img
                src="/bruno-profile.webp"
                alt="Bruno Tachinardi"
                className="size-10 shrink-0 rounded-full object-cover ring-2 ring-primary/30"
              />
              <p className="text-sm font-bold">{t('assignmentBanner.greeting')}</p>
            </div>

            {/* Message body */}
            <p className="text-sm leading-relaxed text-muted-foreground">
              {t('assignmentBanner.message')}
            </p>
            <p className="mt-2 text-sm leading-relaxed text-muted-foreground">
              {t('assignmentBanner.thanks')}
            </p>

            {/* CTAs */}
            <div className="mt-4 flex flex-wrap gap-2">
              <Link
                to="/methodology"
                onClick={dismiss}
                className="rounded-lg bg-primary px-3.5 py-1.5 text-xs font-bold text-primary-foreground transition-all hover:shadow-[0_0_12px_rgba(220,255,2,0.3)]"
              >
                {t('assignmentBanner.ctaMethodology')}
              </Link>
              <a
                href="https://btas.dev"
                target="_blank"
                rel="noopener noreferrer"
                onClick={dismiss}
                className="rounded-lg border border-border/50 px-3.5 py-1.5 text-xs font-bold text-muted-foreground transition-colors hover:text-foreground"
              >
                {t('assignmentBanner.ctaPortfolio')}
              </a>
            </div>
          </div>
        </motion.div>
      )}
    </AnimatePresence>
  );
}
