import { cn } from '@/lib/utils';

interface SectionHeadingProps {
  title: string;
  subtitle?: string;
  /** Highlighted word(s) in highlight color. If present, must be a substring of title. */
  highlight?: string;
  align?: 'left' | 'center';
  className?: string;
}

/** Styled section heading with optional highlighted text and subtitle. */
export function SectionHeading({
  title,
  subtitle,
  highlight,
  align = 'center',
  className,
}: SectionHeadingProps) {
  const renderTitle = () => {
    if (!highlight) {
      return title;
    }
    const idx = title.indexOf(highlight);
    if (idx === -1) return title;
    return (
      <>
        {title.slice(0, idx)}
        <span className="text-highlight">{highlight}</span>
        {title.slice(idx + highlight.length)}
      </>
    );
  };

  return (
    <div className={cn(align === 'center' && 'text-center', className)}>
      <h2 className="text-3xl font-extrabold tracking-tight sm:text-4xl lg:text-5xl">
        {renderTitle()}
      </h2>
      {subtitle && (
        <p className="mx-auto mt-4 max-w-2xl text-base text-muted-foreground sm:text-lg">
          {subtitle}
        </p>
      )}
    </div>
  );
}
