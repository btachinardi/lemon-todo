import { cn } from "@/lib/utils"

/** Animated placeholder for loading content. Pulsing background indicates content is being fetched. */
function Skeleton({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="skeleton"
      className={cn("bg-accent animate-pulse rounded-md", className)}
      {...props}
    />
  )
}

export { Skeleton }
