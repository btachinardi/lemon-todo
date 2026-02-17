import { useTranslation } from 'react-i18next';
import { motion, type Variant } from 'motion/react';
import type { LucideIcon } from 'lucide-react';

interface TierCardProps {
  tierIndex: number;
  icon: LucideIcon;
  featureIcons: LucideIcon[];
  index: number;
}

const hidden: Variant = { opacity: 0, y: 24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.6, delay: i * 0.15, ease: [0.25, 0.46, 0.45, 0.94] },
});

/** Roadmap tier card with header badge, icon, tagline, and feature grid. */
export function TierCard({ tierIndex, icon: TierIcon, featureIcons, index }: TierCardProps) {
  const { t } = useTranslation();

  return (
    <motion.div
      variants={{ hidden, visible: visible(index) }}
      className="group rounded-xl border-2 border-border/50 bg-card/50 p-6 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_24px_rgba(220,255,2,0.08)]"
    >
      {/* Tier header */}
      <div className="mb-5 flex items-start gap-4">
        <div className="flex size-12 shrink-0 items-center justify-center rounded-xl bg-primary/10 text-primary transition-colors group-hover:bg-primary/15">
          <TierIcon className="size-6" />
        </div>
        <div className="min-w-0">
          <span className="mb-1 inline-block rounded-full bg-primary/15 px-2.5 py-0.5 text-[10px] font-black tracking-wider text-primary">
            TIER {tierIndex + 1}
          </span>
          <h3 className="text-lg font-bold leading-tight">
            {t(`roadmap.tiers.${tierIndex}.name`)}
          </h3>
          <p className="text-base text-muted-foreground">
            {t(`roadmap.tiers.${tierIndex}.tagline`)}
          </p>
        </div>
      </div>

      {/* Feature grid */}
      <div className="grid gap-3 sm:grid-cols-2">
        {featureIcons.map((FeatureIcon, fi) => (
          <div
            key={fi}
            className="flex items-start gap-3 rounded-lg border border-border/30 bg-background/40 p-3"
          >
            <div className="flex size-8 shrink-0 items-center justify-center rounded-md bg-primary/10 text-primary">
              <FeatureIcon className="size-4" />
            </div>
            <div className="min-w-0">
              <p className="text-base font-semibold">
                {t(`roadmap.tiers.${tierIndex}.features.${fi}.title`)}
              </p>
              <p className="text-sm leading-relaxed text-muted-foreground">
                {t(`roadmap.tiers.${tierIndex}.features.${fi}.description`)}
              </p>
            </div>
          </div>
        ))}
      </div>
    </motion.div>
  );
}
