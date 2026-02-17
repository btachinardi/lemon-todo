import { useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { motion, useInView, type Variant } from 'motion/react';
import { CheckCircle2Icon, DatabaseIcon, ShieldCheckIcon, ContainerIcon, RefreshCwIcon } from 'lucide-react';
import { SectionHeading } from '../atoms/SectionHeading';

const ease = [0.25, 0.46, 0.45, 0.94] as const;
const hidden: Variant = { opacity: 0, y: 24 };
const visible = (i: number): Variant => ({
  opacity: 1,
  y: 0,
  transition: { duration: 0.5, delay: i * 0.12, ease },
});

interface PipelineDetail {
  title: string;
  description: string;
}

/** Job card matching GitHub Actions styling. */
function JobCard({ name, time }: { name: string; time: string }) {
  return (
    <div className="flex items-center gap-2.5 px-3 py-2">
      <CheckCircle2Icon className="size-4 shrink-0 text-success-foreground" />
      <span className="min-w-0 truncate text-sm font-semibold">{name}</span>
      <span className="ml-auto shrink-0 whitespace-nowrap text-xs text-muted-foreground">{time}</span>
    </div>
  );
}

/** GitHub Actions-style DAG diagram (desktop only). */
function PipelineDiagram() {
  return (
    <div className="relative mx-auto" style={{ maxWidth: '960px', aspectRatio: '960 / 310' }}>
      {/* SVG connection lines */}
      <svg
        viewBox="0 0 960 310"
        className="absolute inset-0 size-full"
        fill="none"
        aria-hidden="true"
      >
        {/* Test Group → Docker Build */}
        <path
          d="M 290 95 H 330 Q 350 95 350 85 H 430"
          stroke="currentColor"
          strokeWidth="2"
          className="text-border/60"
        />
        {/* Docker Build → Deploy */}
        <path
          d="M 630 85 H 730"
          stroke="currentColor"
          strokeWidth="2"
          className="text-border/60"
        />
        {/* SQL Server → Deploy junction (curved L-path) */}
        <path
          d="M 290 245 H 330 Q 355 245 355 220 V 110 Q 355 85 380 85 H 680"
          stroke="currentColor"
          strokeWidth="2"
          className="text-border/60"
        />
        {/* Junction dots */}
        <circle cx="350" cy="85" r="4" fill="currentColor" className="text-border" />
        <circle cx="680" cy="85" r="4" fill="currentColor" className="text-border" />
      </svg>

      {/* Job Group: Backend Tests (SQLite) + Frontend Tests */}
      <div
        className="absolute overflow-hidden rounded-xl border border-border/40 bg-card/60 backdrop-blur-sm"
        style={{ left: '2%', top: '4%', width: '28%', height: '55%' }}
      >
        <div className="flex h-full flex-col justify-center gap-2 py-3">
          <JobCard name="Backend Tests (SQLite)" time="1m 23s" />
          <JobCard name="Frontend Tests" time="1m 35s" />
        </div>
      </div>

      {/* Job: Backend Tests (SQL Server) */}
      <div
        className="absolute overflow-hidden rounded-xl border border-border/40 bg-card/60 backdrop-blur-sm"
        style={{ left: '2%', top: '68%', width: '28%', height: '26%' }}
      >
        <div className="flex h-full items-center">
          <JobCard name="Backend Tests (SQL Server)" time="2m 28s" />
        </div>
      </div>

      {/* Job: Docker Build */}
      <div
        className="absolute overflow-hidden rounded-xl border border-border/40 bg-card/60 backdrop-blur-sm"
        style={{ left: '45%', top: '18%', width: '20.5%', height: '26%' }}
      >
        <div className="flex h-full items-center">
          <JobCard name="Docker Build" time="51s" />
        </div>
      </div>

      {/* Job: Deploy to Azure */}
      <div
        className="absolute overflow-hidden rounded-xl border border-border/40 bg-card/60 backdrop-blur-sm"
        style={{ left: '76%', top: '18%', width: '22%', height: '26%' }}
      >
        <div className="flex h-full items-center">
          <JobCard name="Deploy to Azure" time="2m 32s" />
        </div>
      </div>
    </div>
  );
}

/** Simplified mobile pipeline view. */
function MobilePipeline() {
  const jobs = [
    { name: 'Backend Tests (SQLite)', time: '1m 23s' },
    { name: 'Frontend Tests', time: '1m 35s' },
    { name: 'Backend Tests (SQL Server)', time: '2m 28s' },
    { name: 'Docker Build', time: '51s' },
    { name: 'Deploy to Azure', time: '2m 32s' },
  ];

  return (
    <div className="mx-auto max-w-md space-y-2">
      {jobs.map((job, i) => (
        <div key={i}>
          <div className="rounded-lg border border-border/40 bg-card/60 backdrop-blur-sm">
            <JobCard name={job.name} time={job.time} />
          </div>
          {i < jobs.length - 1 && (
            <div className="flex justify-center py-1">
              <div className="h-4 w-px bg-border/40" />
            </div>
          )}
        </div>
      ))}
    </div>
  );
}

/** CI/CD pipeline visualization with GitHub Actions-style DAG diagram. */
export function DevOpsPipelineSection() {
  const { t } = useTranslation();
  const ref = useRef<HTMLElement>(null);
  const isInView = useInView(ref, { once: true, margin: '-80px' });

  const details = t('devops.pipeline.details', { returnObjects: true }) as PipelineDetail[];
  const detailIcons = [DatabaseIcon, ShieldCheckIcon, ContainerIcon, RefreshCwIcon];

  return (
    <section ref={ref} className="px-4 py-24 sm:px-6">
      <div className="mx-auto max-w-7xl">
        <motion.div
          initial={{ opacity: 0, y: 24 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 24 }}
          transition={{ duration: 0.6, ease }}
        >
          <SectionHeading
            title={t('devops.pipeline.title')}
            subtitle={t('devops.pipeline.subtitle')}
            highlight={t('devops.pipeline.highlight')}
          />
        </motion.div>

        {/* GitHub Actions DAG Diagram */}
        <motion.div
          initial={{ opacity: 0, y: 32 }}
          animate={isInView ? { opacity: 1, y: 0 } : { opacity: 0, y: 32 }}
          transition={{ duration: 0.7, delay: 0.2, ease }}
          className="mt-16"
        >
          {/* Diagram wrapper styled like GitHub Actions */}
          <div className="mx-auto max-w-4xl rounded-2xl border-2 border-border/40 bg-card/30 p-6 shadow-2xl shadow-primary/5 backdrop-blur-md sm:p-8">
            {/* Header: deploy.yml / on: push */}
            <div className="mb-6">
              <p className="text-sm font-bold">deploy.yml</p>
              <p className="text-xs text-muted-foreground">on: push</p>
            </div>

            {/* Desktop diagram */}
            <div className="hidden lg:block">
              <PipelineDiagram />
            </div>

            {/* Mobile simplified view */}
            <div className="lg:hidden">
              <MobilePipeline />
            </div>
          </div>
        </motion.div>

        {/* Detail cards */}
        <motion.div
          initial="hidden"
          animate={isInView ? 'visible' : 'hidden'}
          className="mt-16 grid gap-6 sm:grid-cols-2"
        >
          {details.map((detail, i) => {
            const Icon = detailIcons[i % detailIcons.length];
            return (
              <motion.div
                key={i}
                variants={{ hidden, visible: visible(i + 4) }}
                className="group rounded-xl border-2 border-border/50 bg-card/50 p-6 backdrop-blur-sm transition-all duration-300 hover:border-primary/30 hover:shadow-[0_0_24px_rgba(220,255,2,0.08)]"
              >
                <div className="flex items-center gap-3">
                  <div className="flex size-9 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary transition-colors group-hover:bg-primary/15">
                    <Icon className="size-5" />
                  </div>
                  <h3 className="text-sm font-bold">{detail.title}</h3>
                </div>
                <p className="mt-3 text-sm leading-relaxed text-muted-foreground">{detail.description}</p>
              </motion.div>
            );
          })}
        </motion.div>
      </div>
    </section>
  );
}
