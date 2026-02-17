import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router';
import { useTranslation } from 'react-i18next';
import { motion, AnimatePresence } from 'motion/react';
import { XIcon, MailIcon } from 'lucide-react';

const ease = [0.25, 0.46, 0.45, 0.94] as const;
const SEEN_KEY = 'lemondo-evaluator-seen';

/**
 * Floating avatar bubble (always visible on landing pages) that opens
 * a "Message to Evaluators" modal. Auto-opens once on first visit;
 * after that, evaluators can re-open via the bubble.
 */
export function AssignmentBanner() {
  const { t } = useTranslation();
  const [bubbleVisible, setBubbleVisible] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);

  useEffect(() => {
    // Delay bubble entrance so it appears after page paint
    const timer = setTimeout(() => setBubbleVisible(true), 600);
    return () => clearTimeout(timer);
  }, []);

  useEffect(() => {
    // Auto-open modal on first visit
    try {
      if (bubbleVisible && !localStorage.getItem(SEEN_KEY)) {
        const timer = setTimeout(() => setModalOpen(true), 1200);
        return () => clearTimeout(timer);
      }
    } catch {
      // localStorage unavailable — skip auto-open
    }
  }, [bubbleVisible]);

  const openModal = useCallback(() => setModalOpen(true), []);
  const closeModal = useCallback(() => {
    setModalOpen(false);
    try {
      localStorage.setItem(SEEN_KEY, '1');
    } catch {
      // Ignore
    }
  }, []);

  return (
    <>
      {/* ── Floating avatar bubble ─────────────────────────── */}
      <AnimatePresence>
        {bubbleVisible && !modalOpen && (
          <motion.button
            initial={{ opacity: 0, scale: 0.8, y: 20 }}
            animate={{ opacity: 1, scale: 1, y: 0 }}
            exit={{ opacity: 0, scale: 0.8 }}
            transition={{ duration: 0.4, ease }}
            onClick={openModal}
            className="fixed bottom-6 right-6 z-50 flex items-center gap-2.5 rounded-full border-2 border-primary/30 bg-card/95 py-2 pl-2.5 pr-4 shadow-2xl shadow-primary/10 backdrop-blur-xl transition-all hover:border-primary/50 hover:shadow-primary/20"
            aria-label={t('assignmentBanner.title')}
          >
            <div className="relative">
              <img
                src="/bruno-profile.webp"
                alt="Bruno Tachinardi"
                className="size-9 shrink-0 rounded-full object-cover ring-2 ring-primary/30"
              />
              <MailIcon className="absolute -right-1 -top-1 size-4 rounded-full bg-primary p-0.5 text-primary-foreground" />
            </div>
            <span className="text-sm font-bold text-muted-foreground">
              {t('assignmentBanner.title')}
            </span>
          </motion.button>
        )}
      </AnimatePresence>

      {/* ── Modal overlay ──────────────────────────────────── */}
      <AnimatePresence>
        {modalOpen && (
          <>
            {/* Backdrop */}
            <motion.div
              initial={{ opacity: 0 }}
              animate={{ opacity: 1 }}
              exit={{ opacity: 0 }}
              transition={{ duration: 0.25 }}
              className="fixed inset-0 z-50 bg-background/60 backdrop-blur-sm"
              onClick={closeModal}
              aria-hidden="true"
            />

            {/* Modal panel */}
            <motion.div
              initial={{ opacity: 0, y: 40, scale: 0.96 }}
              animate={{ opacity: 1, y: 0, scale: 1 }}
              exit={{ opacity: 0, y: 20, scale: 0.96 }}
              transition={{ duration: 0.35, ease }}
              className="fixed inset-4 z-50 m-auto flex max-h-[calc(100dvh-2rem)] w-full max-w-lg flex-col overflow-hidden rounded-2xl border-2 border-primary/30 bg-card/98 p-6 shadow-2xl shadow-primary/10 backdrop-blur-xl"
              role="dialog"
              aria-modal="true"
              aria-label={t('assignmentBanner.title')}
            >
              {/* Close */}
              <button
                onClick={closeModal}
                className="absolute right-4 top-4 rounded-full p-1.5 text-muted-foreground transition-colors hover:text-foreground"
                aria-label={t('assignmentBanner.dismiss')}
              >
                <XIcon className="size-4" />
              </button>

              {/* Header */}
              <div className="flex items-center gap-3">
                <img
                  src="/bruno-profile.webp"
                  alt="Bruno Tachinardi"
                  className="size-12 shrink-0 rounded-full object-cover ring-2 ring-primary/30"
                />
                <div>
                  <h2 className="text-lg font-extrabold">{t('assignmentBanner.title')}</h2>
                  <p className="text-base text-muted-foreground">{t('assignmentBanner.greeting')}</p>
                </div>
              </div>

              {/* Body */}
              <div className="mt-5 min-h-0 space-y-3 overflow-y-auto text-base leading-relaxed text-muted-foreground">
                <p>{t('assignmentBanner.message')}</p>
                <p>{t('assignmentBanner.process')}</p>
                <p>{t('assignmentBanner.branding')}</p>
                <p className="font-medium text-foreground">{t('assignmentBanner.personal')}</p>
              </div>

              {/* CTAs */}
              <div className="mt-6 flex flex-wrap gap-3">
                <Link
                  to="/methodology"
                  onClick={closeModal}
                  className="rounded-lg bg-primary px-4 py-2 text-base font-bold text-primary-foreground transition-all hover:shadow-[0_0_16px_rgba(220,255,2,0.3)]"
                >
                  {t('assignmentBanner.ctaMethodology')}
                </Link>
                <a
                  href="https://btas.dev"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="rounded-lg border border-border/50 px-4 py-2 text-base font-bold text-muted-foreground transition-colors hover:text-foreground"
                >
                  {t('assignmentBanner.ctaPortfolio')}
                </a>
              </div>
            </motion.div>
          </>
        )}
      </AnimatePresence>
    </>
  );
}
