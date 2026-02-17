interface AppLoadingScreenProps {
  message?: string;
}

/**
 * Full-screen loading animation shown during app initialization.
 *
 * Displays the LemonDo logo bouncing with ripple effects and a loading message.
 * Used by {@link AuthHydrationProvider} while the silent token refresh is in-flight.
 */
export function AppLoadingScreen({ message = 'Loading...' }: AppLoadingScreenProps) {
  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed inset-0 z-50 flex flex-col items-center justify-center bg-background"
    >
      <div className="relative flex flex-col items-center">
        {/* Bouncing logo */}
        <div className="animate-app-loading-bounce">
          <img
            src="/lemondo-icon.png"
            alt="LemonDo"
            className="size-20 drop-shadow-[0_0_24px_var(--color-brand)]"
          />
        </div>

        {/* Shadow that scales with the bounce */}
        <div className="mt-2 h-2 w-16 animate-app-loading-shadow rounded-full bg-brand/20 blur-sm" />

        {/* Ripple container — positioned at the "floor" */}
        <div className="absolute bottom-1 left-1/2 -translate-x-1/2">
          <div
            data-testid="ripple"
            className="absolute left-1/2 top-1/2 size-8 -translate-x-1/2 -translate-y-1/2 animate-app-loading-ripple rounded-full border border-brand/40"
          />
          <div
            data-testid="ripple"
            className="absolute left-1/2 top-1/2 size-8 -translate-x-1/2 -translate-y-1/2 animate-app-loading-ripple rounded-full border border-brand/30 [animation-delay:0.4s]"
          />
          <div
            data-testid="ripple"
            className="absolute left-1/2 top-1/2 size-8 -translate-x-1/2 -translate-y-1/2 animate-app-loading-ripple rounded-full border border-brand/20 [animation-delay:0.8s]"
          />
        </div>
      </div>

      {/* Loading text */}
      <p className="mt-10 animate-pulse font-brand text-base tracking-wide text-muted-foreground">
        {message}
      </p>
    </div>
  );
}
