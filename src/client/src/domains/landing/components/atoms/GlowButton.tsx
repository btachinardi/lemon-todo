import type { ComponentProps, ReactNode } from 'react';
import { Link } from 'react-router';
import { cn } from '@/lib/utils';

type GlowButtonVariant = 'primary' | 'outline';

interface GlowButtonBaseProps {
  variant?: GlowButtonVariant;
  children: ReactNode;
  className?: string;
}

interface GlowButtonLinkProps extends GlowButtonBaseProps {
  /** Internal route path — renders a React Router Link. */
  to: string;
  href?: never;
  target?: never;
  rel?: never;
}

interface GlowButtonAnchorProps extends GlowButtonBaseProps, Omit<ComponentProps<'a'>, 'children' | 'className'> {
  /** External URL — renders a native anchor. */
  href: string;
  to?: never;
}

type GlowButtonProps = GlowButtonLinkProps | GlowButtonAnchorProps;

const baseClasses =
  'inline-flex items-center justify-center gap-2 rounded-lg px-6 py-3 text-sm font-bold tracking-wide transition-all duration-300 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-ring';

const primaryClasses =
  'bg-primary text-primary-foreground hover:shadow-[0_0_32px_rgba(220,255,2,0.4)] hover:scale-[1.02] animate-[highlight-pulse_3s_ease-in-out_infinite]';

const outlineClasses =
  'border-2 border-border bg-transparent text-foreground hover:border-primary/50 hover:text-primary hover:shadow-[0_0_20px_rgba(220,255,2,0.15)]';

/** CTA button with pulsing highlight glow. Use `to` for internal routes, `href` for external links. */
export function GlowButton(props: GlowButtonProps) {
  const { variant = 'primary', className, children } = props;
  const variantClasses = variant === 'primary' ? primaryClasses : outlineClasses;

  if ('to' in props && props.to) {
    return (
      <Link to={props.to} className={cn(baseClasses, variantClasses, className)}>
        {children}
      </Link>
    );
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars -- discriminant props excluded from spread
  const { to, variant: _variant, children: _children, className: _cls, ...anchorProps } = props as GlowButtonAnchorProps & { variant?: GlowButtonVariant; to?: never };
  return (
    <a {...anchorProps} className={cn(baseClasses, variantClasses, className)}>
      {children}
    </a>
  );
}
