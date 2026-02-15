import type { Metric } from 'web-vitals';
import { captureInfo } from './error-logger';

/**
 * Reports a Web Vital metric to the structured logger.
 * In production, this is where you'd send to an analytics endpoint.
 */
function reportMetric(metric: Metric): void {
  captureInfo(`Web Vital: ${metric.name} = ${metric.value.toFixed(1)}`, {
    source: 'WebVitals',
    metadata: {
      name: metric.name,
      value: metric.value,
      rating: metric.rating,
      delta: metric.delta,
      id: metric.id,
      navigationType: metric.navigationType,
    },
  });
}

/**
 * Initializes Web Vitals collection for CLS, FCP, FID, INP, LCP, and TTFB.
 * Uses dynamic import to avoid adding to the critical bundle path.
 * Call once at app startup.
 */
export async function initWebVitals(): Promise<void> {
  const { onCLS, onFCP, onINP, onLCP, onTTFB } = await import('web-vitals');

  onCLS(reportMetric);
  onFCP(reportMetric);
  onINP(reportMetric);
  onLCP(reportMetric);
  onTTFB(reportMetric);
}
