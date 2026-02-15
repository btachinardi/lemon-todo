import { toast } from 'sonner';
import { ApiRequestError } from '@/lib/api-client';

/**
 * Shows a toast with a message derived from the API error.
 * Uses the structured error details when available, falls back to a generic message.
 */
export function toastApiError(error: Error, fallback: string): void {
  if (error instanceof ApiRequestError) {
    if (error.status === 404) {
      toast.error('Item not found. It may have been deleted.');
      return;
    }
    if (error.status === 400 || error.status === 422) {
      toast.error(error.apiError.title || fallback);
      return;
    }
  }
  toast.error(fallback);
}
