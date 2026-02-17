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
        {/* Icon + ripple wrapper */}
        <div className="relative">
          {/* Ripples — centered on icon, rendered behind via natural stacking order */}
          <div
            data-testid="ripple"
            className="pointer-events-none absolute inset-0 m-auto h-4 w-8 animate-app-loading-ripple rounded-full border border-brand/20"
          />
          <div
            data-testid="ripple"
            className="pointer-events-none absolute inset-0 m-auto h-4 w-8 animate-app-loading-ripple rounded-full border border-brand/15 [animation-delay:0.4s]"
          />
          <div
            data-testid="ripple"
            className="pointer-events-none absolute inset-0 m-auto h-4 w-8 animate-app-loading-ripple rounded-full border border-brand/10 [animation-delay:0.8s]"
          />

          {/* Bouncing logo — above ripples */}
          <div className="relative z-10 animate-app-loading-bounce">
            <img
              src="/lemondo-icon.png"
              alt="LemonDo"
              className="size-20 drop-shadow-[0_0_24px_var(--color-brand)]"
            />
          </div>
        </div>

        {/* Shadow that scales with the bounce */}
        <div className="mt-2 h-2 w-16 animate-app-loading-shadow rounded-full bg-brand/20 blur-sm" />
      </div>

      {/* Loading text */}
      <p className="mt-10 animate-pulse font-brand text-base tracking-wide text-muted-foreground">
        {message}
      </p>
    </div>
  );
}
