import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

/**
 * Merges class names with Tailwind CSS conflict resolution.
 * Uses clsx for conditional classes, then tailwind-merge to resolve
 * Tailwind utility conflicts (e.g., "p-4 p-2" becomes "p-2").
 *
 * @param inputs - Class names, objects, or arrays to merge.
 * @returns A single space-separated string with conflicts resolved.
 */
export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
