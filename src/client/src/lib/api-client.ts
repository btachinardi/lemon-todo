import type { ApiError } from '@/domains/tasks/types/api.types';

/** Accepted value types for query-string parameters. */
type ParamValue = string | number | boolean | null | undefined;

/** Default request timeout in milliseconds. */
const REQUEST_TIMEOUT_MS = 15_000;

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

/** Handles responses that return a JSON body (non-204). */
async function handleJsonResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    await throwApiError(response);
  }

  return response.json() as Promise<T>;
}

/** Handles responses with no body (204 No Content, or any void endpoint). */
async function handleVoidResponse(response: Response): Promise<void> {
  if (!response.ok) {
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

    const response = await fetch(fullUrl, {
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
      },
      signal: createTimeoutSignal(),
    });
    return handleJsonResponse<T>(response);
  },

  /** Sends a POST request with a JSON body. Returns the parsed response. */
  async post<T>(url: string, body?: unknown): Promise<T> {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
      },
      signal: createTimeoutSignal(),
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return handleJsonResponse<T>(response);
  },

  /** Sends a POST request for action endpoints that return no body. */
  async postVoid(url: string, body?: unknown): Promise<void> {
    const response = await fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
      },
      signal: createTimeoutSignal(),
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return handleVoidResponse(response);
  },

  /** Sends a PUT request with a JSON body. Returns the parsed response. */
  async put<T>(url: string, body?: unknown): Promise<T> {
    const response = await fetch(url, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
      },
      signal: createTimeoutSignal(),
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
    return handleJsonResponse<T>(response);
  },

  /** Sends a DELETE request. Returns the parsed response body. */
  async delete<T>(url: string): Promise<T> {
    const response = await fetch(url, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
      },
      signal: createTimeoutSignal(),
    });
    return handleJsonResponse<T>(response);
  },

  /** Sends a DELETE request for endpoints that return no body. */
  async deleteVoid(url: string): Promise<void> {
    const response = await fetch(url, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        'X-Correlation-Id': generateCorrelationId(),
      },
      signal: createTimeoutSignal(),
    });
    return handleVoidResponse(response);
  },
};
