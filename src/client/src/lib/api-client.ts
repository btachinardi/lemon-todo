import type { ApiError } from '@/domains/tasks/types/api.types';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';
import { attemptTokenRefresh } from './token-refresh';

/** Accepted value types for query-string parameters. */
type ParamValue = string | number | boolean | null | undefined;

/** Default request timeout in milliseconds. */
const REQUEST_TIMEOUT_MS = 15_000;

/**
 * Returns the access token from the in-memory Zustand auth store.
 * No localStorage reads — tokens live only in JS memory.
 */
function getAccessToken(): string | null {
  return useAuthStore.getState().accessToken;
}

/**
 * Creates an AbortSignal that automatically aborts after the given timeout.
 * Uses the native `AbortSignal.timeout()` API available in modern browsers.
 */
function createTimeoutSignal(ms: number = REQUEST_TIMEOUT_MS): AbortSignal {
  return AbortSignal.timeout(ms);
}

/** Generates a short unique ID for request correlation. */
function generateCorrelationId(): string {
  return crypto.randomUUID().replace(/-/g, '');
}

/** Builds common request headers including auth bearer token when available. */
function buildHeaders(): Record<string, string> {
  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
    'X-Correlation-Id': generateCorrelationId(),
  };

  const token = getAccessToken();
  if (token) {
    headers['Authorization'] = `Bearer ${token}`;
  }

  return headers;
}

/**
 * Runtime guard that proves an unknown value conforms to the {@link ApiError}
 * shape (RFC 7807 problem details). Used at the fetch boundary so we never
 * lie to the compiler about what the server returned.
 */
function isApiError(value: unknown): value is ApiError {
  if (typeof value !== 'object' || value === null) return false;
  if (!('type' in value) || !('title' in value) || !('status' in value)) return false;

  // After the `in` checks TypeScript narrows to Record<'type'|'title'|'status', unknown>,
  // so property access is safe without casts.
  return (
    typeof value.type === 'string' &&
    typeof value.title === 'string' &&
    typeof value.status === 'number'
  );
}

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

/**
 * Parses a non-2xx response into an {@link ApiRequestError}.
 * Validates the body at runtime -- if the server returns something that
 * does not match the RFC 7807 shape, a fallback error is constructed
 * from the HTTP status line.
 */
async function throwApiError(response: Response): Promise<never> {
  const body: unknown = await response.json().catch(() => undefined);

  const apiError: ApiError = isApiError(body)
    ? body
    : {
        type: 'unknown_error',
        title: response.statusText || 'Request failed',
        status: response.status,
      };

  throw new ApiRequestError(response.status, apiError);
}

/** Whether a URL is an auth endpoint (should not trigger token refresh). */
function isAuthEndpoint(url: string): boolean {
  return url.includes('/api/auth/');
}

/**
 * Handles a 401 response by attempting a token refresh, then retrying the request.
 * Skips retry for auth endpoints to avoid infinite loops.
 * Returns the retried response on success, or throws ApiRequestError on failure.
 */
async function handleUnauthorizedWithRefresh(
  response: Response,
  retryFn: () => Promise<Response>,
): Promise<Response | null> {
  if (response.status !== 401 || isAuthEndpoint(response.url)) {
    return null;
  }

  const refreshResult = await attemptTokenRefresh();
  if (!refreshResult) {
    // Refresh failed — redirect already happened in attemptTokenRefresh
    await throwApiError(response);
  }

  // Retry the original request with the new token
  return retryFn();
}

/** Handles responses that return a JSON body (non-204). */
async function handleJsonResponse<T>(
  response: Response,
  retryFn: () => Promise<Response>,
): Promise<T> {
  if (!response.ok) {
    const retried = await handleUnauthorizedWithRefresh(response, retryFn);
    if (retried) {
      if (!retried.ok) await throwApiError(retried);
      return retried.json() as Promise<T>;
    }
    await throwApiError(response);
  }

  return response.json() as Promise<T>;
}

/** Handles responses with no body (204 No Content, or any void endpoint). */
async function handleVoidResponse(
  response: Response,
  retryFn: () => Promise<Response>,
): Promise<void> {
  if (!response.ok) {
    const retried = await handleUnauthorizedWithRefresh(response, retryFn);
    if (retried) {
      if (!retried.ok) await throwApiError(retried);
      return;
    }
    await throwApiError(response);
  }
}

/**
 * Thin fetch wrapper with JSON serialization and typed error handling.
 * All methods throw {@link ApiRequestError} on non-2xx responses.
 *
 * Uses the Vite dev server proxy in development -- all URLs are relative
 * (e.g. `/api/tasks`), no base URL configuration needed.
 *
 * On 401 responses (except from auth endpoints), automatically attempts
 * a token refresh and retries the original request once.
 *
 * All requests include `credentials: 'include'` so the HttpOnly refresh
 * token cookie is sent on `/api/auth/*` paths (cookie is scoped to
 * `/api/auth` so it won't be sent on other paths).
 *
 * Methods suffixed with `Void` are for endpoints that return no body
 * (204 No Content or action endpoints). This eliminates the need for
 * `undefined as T` casts.
 */
export const apiClient = {
  /** Sends a GET request. Query params with `undefined`/`null`/empty values are omitted. */
  async get<T>(url: string, params?: Record<string, ParamValue>): Promise<T> {
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

    const doFetch = () =>
      fetch(fullUrl, {
        headers: buildHeaders(),
        signal: createTimeoutSignal(),
        credentials: 'include',
      });

    const response = await doFetch();
    return handleJsonResponse<T>(response, doFetch);
  },

  /** Sends a POST request with a JSON body. Returns the parsed response. */
  async post<T>(url: string, body?: unknown): Promise<T> {
    const doFetch = () =>
      fetch(url, {
        method: 'POST',
        headers: buildHeaders(),
        signal: createTimeoutSignal(),
        credentials: 'include',
        body: body !== undefined ? JSON.stringify(body) : undefined,
      });

    const response = await doFetch();
    return handleJsonResponse<T>(response, doFetch);
  },

  /** Sends a POST request for action endpoints that return no body. */
  async postVoid(url: string, body?: unknown): Promise<void> {
    const doFetch = () =>
      fetch(url, {
        method: 'POST',
        headers: buildHeaders(),
        signal: createTimeoutSignal(),
        credentials: 'include',
        body: body !== undefined ? JSON.stringify(body) : undefined,
      });

    const response = await doFetch();
    return handleVoidResponse(response, doFetch);
  },

  /** Sends a PUT request with a JSON body. Returns the parsed response. */
  async put<T>(url: string, body?: unknown): Promise<T> {
    const doFetch = () =>
      fetch(url, {
        method: 'PUT',
        headers: buildHeaders(),
        signal: createTimeoutSignal(),
        credentials: 'include',
        body: body !== undefined ? JSON.stringify(body) : undefined,
      });

    const response = await doFetch();
    return handleJsonResponse<T>(response, doFetch);
  },

  /** Sends a DELETE request. Returns the parsed response body. */
  async delete<T>(url: string): Promise<T> {
    const doFetch = () =>
      fetch(url, {
        method: 'DELETE',
        headers: buildHeaders(),
        signal: createTimeoutSignal(),
        credentials: 'include',
      });

    const response = await doFetch();
    return handleJsonResponse<T>(response, doFetch);
  },

  /** Sends a DELETE request for endpoints that return no body. */
  async deleteVoid(url: string): Promise<void> {
    const doFetch = () =>
      fetch(url, {
        method: 'DELETE',
        headers: buildHeaders(),
        signal: createTimeoutSignal(),
        credentials: 'include',
      });

    const response = await doFetch();
    return handleVoidResponse(response, doFetch);
  },
};
