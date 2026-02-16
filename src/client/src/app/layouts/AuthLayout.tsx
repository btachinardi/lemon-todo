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
        <div className="text-center">
          <h1 className="font-mono text-3xl font-light tracking-normal">
            <span className="text-foreground">LEMON</span>
            <span className="text-primary">DO</span>
          </h1>
        </div>
        {children}
      </div>
    </div>
  );
}
