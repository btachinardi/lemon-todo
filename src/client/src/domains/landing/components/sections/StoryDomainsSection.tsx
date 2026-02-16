import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import {
  CheckSquareIcon,
  LayoutDashboardIcon,
  FingerprintIcon,
  ShieldCheckIcon,
  BellRingIcon,
} from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';
import { cn } from '@/lib/utils';

/* ── Node data ────────────────────────────────────────────────── */

interface DomainNode {
  id: string;
  icon: React.ComponentType<{ className?: string }>;
  /** Position in the constellation (% of container) */
  x: number;
  y: number;
  /** Code-level entity names (not translated) */
  entities: string[];
  /** Tailwind classes */
  border: string;
  iconBg: string;
  iconText: string;
  glow: string;
  /** SVG color for flowing dots */
  svgColor: string;
}

const NODES: DomainNode[] = [
  {
    id: 'task',
    icon: CheckSquareIcon,
    x: 50,
    y: 16,
    entities: ['Task', 'TaskTitle', 'Priority', 'Tag', 'DueDate', 'SensitiveNote'],
    border: 'border-yellow-400/25 hover:border-yellow-400/50',
    iconBg: 'bg-yellow-400/10',
    iconText: 'text-yellow-400',
    glow: 'hover:shadow-[0_0_30px_rgba(250,204,21,0.12)]',
    svgColor: 'rgba(250,204,21,0.5)',
  },
  {
    id: 'board',
    icon: LayoutDashboardIcon,
    x: 76,
    y: 46,
    entities: ['Board', 'Column', 'TaskCard', 'Position'],
    border: 'border-sky-400/25 hover:border-sky-400/50',
    iconBg: 'bg-sky-400/10',
    iconText: 'text-sky-400',
    glow: 'hover:shadow-[0_0_30px_rgba(56,189,248,0.12)]',
    svgColor: 'rgba(56,189,248,0.5)',
  },
  {
    id: 'identity',
    icon: FingerprintIcon,
    x: 24,
    y: 46,
    entities: ['User', 'Email', 'DisplayName', 'RefreshToken'],
    border: 'border-violet-400/25 hover:border-violet-400/50',
    iconBg: 'bg-violet-400/10',
    iconText: 'text-violet-400',
    glow: 'hover:shadow-[0_0_30px_rgba(167,139,250,0.12)]',
    svgColor: 'rgba(167,139,250,0.5)',
  },
  {
    id: 'admin',
    icon: ShieldCheckIcon,
    x: 32,
    y: 78,
    entities: ['AuditEntry', 'AuditAction', 'RequestContext'],
    border: 'border-amber-400/25 hover:border-amber-400/50',
    iconBg: 'bg-amber-400/10',
    iconText: 'text-amber-400',
    glow: 'hover:shadow-[0_0_30px_rgba(251,191,36,0.12)]',
    svgColor: 'rgba(251,191,36,0.5)',
  },
  {
    id: 'notification',
    icon: BellRingIcon,
    x: 68,
    y: 78,
    entities: ['Notification', 'NotificationType', 'PushSubscription'],
    border: 'border-emerald-400/25 hover:border-emerald-400/50',
    iconBg: 'bg-emerald-400/10',
    iconText: 'text-emerald-400',
    glow: 'hover:shadow-[0_0_30px_rgba(52,211,153,0.12)]',
    svgColor: 'rgba(52,211,153,0.5)',
  },
];

/* ── Connection data ──────────────────────────────────────────── */

const CONNECTIONS = [
  { from: 'task', to: 'board' },
  { from: 'identity', to: 'task' },
  { from: 'identity', to: 'board' },
  { from: 'task', to: 'notification' },
  { from: 'identity', to: 'admin' },
];

/* ── SVG helpers ──────────────────────────────────────────────── */

const VB_W = 1000;
const VB_H = 750;

function nodeById(id: string) {
  return NODES.find((n) => n.id === id)!;
}

function toSvg(node: DomainNode) {
  return { x: (node.x / 100) * VB_W, y: (node.y / 100) * VB_H };
}

function connectionPath(from: string, to: string) {
  const a = toSvg(nodeById(from));
  const b = toSvg(nodeById(to));
  const mx = (a.x + b.x) / 2;
  const my = (a.y + b.y) / 2;
  const dx = b.x - a.x;
  const dy = b.y - a.y;
  const len = Math.sqrt(dx * dx + dy * dy);
  const offset = len * 0.12;
  const nx = -dy / len;
  const ny = dx / len;
  return `M ${a.x},${a.y} Q ${mx + nx * offset},${my + ny * offset} ${b.x},${b.y}`;
}

/* ── Animation variants ───────────────────────────────────────── */

const ease = [0.25, 0.46, 0.45, 0.94] as const;

const nodeHidden: Variant = { opacity: 0, scale: 0.85 };
const nodeVisible = (i: number): Variant => ({
  opacity: 1,
  scale: 1,
  transition: { duration: 0.6, delay: 0.2 + i * 0.1, ease },
});

const cardHidden: Variant = { opacity: 0, y: 24 };
const cardVisible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.5, delay: 0.1 + i * 0.08, ease },
});

/* ── Component ────────────────────────────────────────────────── */

/** Interactive DDD context map with animated connections and flowing data particles. */
export function StoryDomainsSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  return (
    <section ref={ref} className="bg-muted/30 px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        {/* Heading */}
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease }}
        >
          <SectionHeading
            title={t('story.domains.title')}
            subtitle={t('story.domains.subtitle')}
            highlight={t('story.domains.highlight')}
          />
        </motion.div>

        {/* ── Desktop: Constellation Map ──────────────── */}
        <div className="relative mt-16 hidden lg:block">
          <div
            className="relative mx-auto overflow-hidden rounded-2xl border-2 border-border/30 bg-background/80 backdrop-blur-sm"
            style={{ aspectRatio: `${VB_W}/${VB_H}` }}
          >
            {/* Dot grid background */}
            <div
              className="pointer-events-none absolute inset-0 opacity-[0.04]"
              aria-hidden="true"
              style={{
                backgroundImage: 'radial-gradient(circle, currentColor 1px, transparent 1px)',
                backgroundSize: '20px 20px',
              }}
            />

            {/* SVG connection layer */}
            <svg
              className="absolute inset-0 h-full w-full"
              viewBox={`0 0 ${VB_W} ${VB_H}`}
              preserveAspectRatio="xMidYMid meet"
              aria-hidden="true"
            >
              <defs>
                <filter id="domains-dot-glow" x="-100%" y="-100%" width="300%" height="300%">
                  <feGaussianBlur in="SourceGraphic" stdDeviation="6" />
                </filter>
              </defs>

              {CONNECTIONS.map((conn, i) => {
                const d = connectionPath(conn.from, conn.to);
                const pathId = `dpath-${conn.from}-${conn.to}`;
                const fromNode = nodeById(conn.from);
                const dur = `${3 + i * 0.7}s`;

                return (
                  <g key={pathId}>
                    {/* Connection line (animated draw) */}
                    <motion.path
                      id={pathId}
                      d={d}
                      fill="none"
                      stroke="currentColor"
                      strokeWidth="2"
                      className="text-muted-foreground/[0.08]"
                      initial={{ pathLength: 0, opacity: 0 }}
                      animate={
                        isInView
                          ? { pathLength: 1, opacity: 1 }
                          : { pathLength: 0, opacity: 0 }
                      }
                      transition={{ duration: 1.2, delay: 0.5 + i * 0.15, ease }}
                    />

                    {/* Flowing data particles */}
                    {isInView && (
                      <>
                        <circle
                          r="8"
                          fill={fromNode.svgColor}
                          opacity="0.25"
                          filter="url(#domains-dot-glow)"
                        >
                          <animateMotion dur={dur} repeatCount="indefinite">
                            <mpath href={`#${pathId}`} />
                          </animateMotion>
                        </circle>
                        <circle r="3.5" fill={fromNode.svgColor} opacity="0.7">
                          <animateMotion dur={dur} repeatCount="indefinite">
                            <mpath href={`#${pathId}`} />
                          </animateMotion>
                        </circle>
                      </>
                    )}
                  </g>
                );
              })}
            </svg>

            {/* Positioned node cards */}
            <motion.div initial="hidden" animate={isInView ? 'visible' : 'hidden'}>
              {NODES.map((node, i) => {
                const Icon = node.icon;
                return (
                  <motion.div
                    key={node.id}
                    variants={{ hidden: nodeHidden, visible: nodeVisible(i) }}
                    className="absolute -translate-x-1/2 -translate-y-1/2"
                    style={{ left: `${node.x}%`, top: `${node.y}%` }}
                  >
                    <div
                      className={cn(
                        'w-56 rounded-xl border-2 bg-card/80 p-4 backdrop-blur-md transition-all duration-300',
                        node.border,
                        node.glow,
                      )}
                    >
                      <div className="flex items-center gap-3">
                        <div
                          className={cn(
                            'flex size-10 shrink-0 items-center justify-center rounded-lg',
                            node.iconBg,
                          )}
                        >
                          <Icon className={cn('size-5', node.iconText)} />
                        </div>
                        <div className="min-w-0">
                          <p className="text-base font-bold leading-tight">
                            {t(`story.domains.contexts.${node.id}.name`)}
                          </p>
                          <p
                            className={cn(
                              'text-[11px] font-semibold uppercase tracking-wider',
                              node.iconText,
                            )}
                          >
                            {t(`story.domains.contexts.${node.id}.role`)}
                          </p>
                        </div>
                      </div>
                      <div className="mt-3 flex flex-wrap gap-1">
                        {node.entities.slice(0, 4).map((e) => (
                          <span
                            key={e}
                            className="rounded-full border border-border/30 bg-muted/20 px-2 py-0.5 font-mono text-[10px] text-muted-foreground/70"
                          >
                            {e}
                          </span>
                        ))}
                        {node.entities.length > 4 && (
                          <span className="rounded-full border border-border/30 bg-muted/20 px-2 py-0.5 font-mono text-[10px] text-muted-foreground/70">
                            +{node.entities.length - 4}
                          </span>
                        )}
                      </div>
                    </div>
                  </motion.div>
                );
              })}
            </motion.div>
          </div>
        </div>

        {/* ── Mobile: Stacked cards ──────────────────── */}
        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-4 sm:grid-cols-2 lg:hidden"
        >
          {NODES.map((node, i) => {
            const Icon = node.icon;
            return (
              <motion.div
                key={node.id}
                variants={{ hidden: cardHidden, visible: cardVisible(i) }}
                className={cn(
                  'rounded-xl border-2 bg-card/50 p-5 backdrop-blur-sm transition-colors duration-300',
                  node.border,
                )}
              >
                <div className="flex items-center gap-3">
                  <div
                    className={cn(
                      'flex size-10 shrink-0 items-center justify-center rounded-lg',
                      node.iconBg,
                    )}
                  >
                    <Icon className={cn('size-5', node.iconText)} />
                  </div>
                  <div>
                    <h4 className="text-base font-bold">
                      {t(`story.domains.contexts.${node.id}.name`)}
                    </h4>
                    <p
                      className={cn(
                        'text-xs font-semibold uppercase tracking-wider',
                        node.iconText,
                      )}
                    >
                      {t(`story.domains.contexts.${node.id}.role`)}
                    </p>
                  </div>
                </div>
                <p className="mt-3 text-sm leading-relaxed text-muted-foreground">
                  {t(`story.domains.contexts.${node.id}.description`)}
                </p>
                <div className="mt-3 flex flex-wrap gap-1.5">
                  {node.entities.map((e) => (
                    <span
                      key={e}
                      className="rounded-full border border-border/30 bg-muted/20 px-2 py-0.5 font-mono text-[10px] text-muted-foreground/70"
                    >
                      {e}
                    </span>
                  ))}
                </div>
              </motion.div>
            );
          })}
        </motion.div>

        {/* Insight callout */}
        <motion.div
          initial={{ opacity: 0, y: 16 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 16 }}
          transition={{ duration: 0.6, delay: 1.0, ease }}
          className="mx-auto mt-12 max-w-2xl border-l-4 border-primary/40 pl-6"
        >
          <p className="text-lg italic text-muted-foreground">
            &ldquo;{t('story.domains.insight')}&rdquo;
          </p>
        </motion.div>
      </div>
    </section>
  );
}
