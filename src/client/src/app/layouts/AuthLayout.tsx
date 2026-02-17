import type { ReactNode } from 'react';

/** Props for {@link AuthLayout}. */
interface AuthLayoutProps {
  children: ReactNode;
}

/** Centered auth layout with branding. Used for login and register pages. */
export function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-background p-4">
      <div className="w-full max-w-md space-y-8">
        <div className="flex flex-col items-center gap-3">
          <img src="/lemondo-icon.png" alt="" className="size-24" />
          <h1 className="font-[var(--font-brand)] text-4xl font-black tracking-tight">
            <span className="text-foreground">Lemon.</span>
            <span className="text-primary">DO</span>
          </h1>
        </div>
        {children}
      </div>
    </div>
  );
}
