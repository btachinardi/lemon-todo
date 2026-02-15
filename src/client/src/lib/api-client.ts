import type { ApiError } from '@/domains/tasks/types/api.types';

/**
 * Typed error thrown when the API returns a non-2xx response.
 * Wraps the RFC 7807 problem details body for structured error handling.
 *
 * @example
 * ```ts
 * try {
 *   await apiClient.post('/api/tasks', payload);
 * } catch (err) {
 *   if (err instanceof ApiRequestError && err.status === 422) {
 *     // handle validation errors via err.apiError.errors
 *   }
 * }
 * ```
 */
export class ApiRequestError extends Error {
  status: number;
  apiError: ApiError;

  constructor(status: number, apiError: ApiError) {
    super(apiError.title);
    this.name = 'ApiRequestError';
    this.status = status;
    this.apiError = apiError;
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => ({
      type: 'unknown_error',
      title: response.statusText,
      status: response.status,
    }));
    throw new ApiRequestError(response.status, body as ApiError);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return response.json() as Promise<T>;
}

/**
 * Thin fetch wrapper with JSON serialization and typed error handling.
 * All methods throw {@link ApiRequestError} on non-2xx responses.
 *
 * Uses the Vite dev server proxy in development -- all URLs are relative
 * (e.g. `/api/tasks`), no base URL configuration needed.
 */
export const apiClient = {
  async get<T>(url: string, params?: Record<string, string | number | undefined>): Promise<T> {
    const searchParams = new URLSearchParams();
    if (params) {
      for (const [key, value] of Object.entries(params)) {
        if (value !== undefined && value !== null && value !== '') {
          searchParams.set(key, String(value));
        }
      }
    }
    const queryString = searchParams.toString();
    const fullUrl = queryString ? `${url}?${queryString}` : url;

    const response = await fetch(fullUrl, {
      headers: { 'Content-Type': 'application/json' },
    });
    return handleResponse<T>(response);
  },

  async post<T>(url: string, body?: unknown): Promise<T> {
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return handleResponse<T>(response);
  },

  async put<T>(url: string, body?: unknown): Promise<T> {
    const response = await fetch(url, {
      method: 'PUT',
      headers: { 'Content-Type': 'application/json' },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return handleResponse<T>(response);
  },

  async delete<T>(url: string): Promise<T> {
    const response = await fetch(url, {
      method: 'DELETE',
      headers: { 'Content-Type': 'application/json' },
    });
    return handleResponse<T>(response);
  },
};
