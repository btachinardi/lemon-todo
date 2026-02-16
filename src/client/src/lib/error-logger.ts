/**
 * Lightweight structured logger that captures errors, warnings, info, and debug
 * messages with context. In development, all levels log to console.
 * Designed as a seam for future integration with an error reporting service.
 */

type LogLevel = 'error' | 'warn' | 'info' | 'debug';

interface ErrorContext {
  /** Where the log originated (e.g., 'ErrorBoundary', 'unhandledrejection', 'mutation') */
  source: string;
  /** Additional metadata about the event */
  metadata?: Record<string, unknown>;
}

const isDev = import.meta.env.DEV;
let isEnabled = isDev;

/**
 * Enable or disable logging at runtime. Useful for toggling
 * diagnostics in production via a support tool or feature flag.
 */
export function setLoggerEnabled(enabled: boolean): void {
  isEnabled = enabled;
}

function log(level: LogLevel, messageOrError: unknown, context: ErrorContext): void {
  if (!isEnabled) return;

  const prefix = `[${context.source}]`;
  const meta = context.metadata && Object.keys(context.metadata).length > 0 ? context.metadata : undefined;

  switch (level) {
    case 'error':
      console.error(prefix, messageOrError, ...(meta ? [meta] : []));
      break;
    case 'warn':
      console.warn(prefix, messageOrError, ...(meta ? [meta] : []));
      break;
    case 'info':
      console.info(prefix, messageOrError, ...(meta ? [meta] : []));
      break;
    case 'debug':
      console.debug(prefix, messageOrError, ...(meta ? [meta] : []));
      break;
  }
}

/**
 * Captures an error with structured context. In development, logs to console.
 * This is the single seam where a reporting service (Sentry, Datadog, etc.)
 * would be wired in.
 *
 * @param error - The error to capture (Error instance or any thrown value).
 * @param context - Contextual metadata about where and why the error occurred.
 */
export function captureError(error: unknown, context: ErrorContext): void {
  log('error', error, context);
}

/**
 * Captures a warning-level message with context. Used for non-fatal issues
 * like failed background refreshes or degraded functionality.
 *
 * @param message - The warning message describing the issue.
 * @param context - Contextual metadata about where the warning originated.
 */
export function captureWarning(message: string, context: ErrorContext): void {
  log('warn', message, context);
}

/**
 * Captures an informational message. Used for significant user actions
 * or lifecycle events worth tracking.
 *
 * @param message - The informational message to log.
 * @param context - Contextual metadata about the event.
 */
export function captureInfo(message: string, context: ErrorContext): void {
  log('info', message, context);
}

/**
 * Captures a debug-level message. Used for verbose diagnostics
 * during development.
 *
 * @param message - The debug message for diagnostics.
 * @param context - Contextual metadata about the debug event.
 */
export function captureDebug(message: string, context: ErrorContext): void {
  log('debug', message, context);
}
