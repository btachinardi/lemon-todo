/**
 * @vitest-environment jsdom
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

describe('attemptTokenRefresh', () => {
  const originalLocation = window.location;

  beforeEach(() => {
    vi.restoreAllMocks();
    vi.resetModules();

    Object.defineProperty(window, 'location', {
      writable: true,
      value: { ...originalLocation, href: '/' },
    });
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    Object.defineProperty(window, 'location', {
      writable: true,
      value: originalLocation,
    });
  });

  it('should refresh tokens on success', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: true,
      json: () =>
        Promise.resolve({
          accessToken: 'new-access',
          user: { id: '1', email: 'a@b.com', displayName: 'Test' },
        }),
    } as Response);

    const { attemptTokenRefresh } = await import('../token-refresh');
    const result = await attemptTokenRefresh();

    expect(result).not.toBeNull();
    expect(result!.accessToken).toBe('new-access');

    // Verify fetch was called with credentials: include
    expect(globalThis.fetch).toHaveBeenCalledWith('/api/auth/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
    });
  });

  it('should redirect to /login on refresh failure', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: false,
      status: 401,
    } as Response);

    const { attemptTokenRefresh } = await import('../token-refresh');
    const result = await attemptTokenRefresh();

    expect(result).toBeNull();
    expect(window.location.href).toBe('/login');
  });

  it('should redirect to /login on network error', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValueOnce(new Error('Network error'));

    const { attemptTokenRefresh } = await import('../token-refresh');
    const result = await attemptTokenRefresh();

    expect(result).toBeNull();
    expect(window.location.href).toBe('/login');
  });

  it('should deduplicate concurrent refresh calls', async () => {
    let fetchCallCount = 0;
    vi.spyOn(globalThis, 'fetch').mockImplementation(
      () =>
        new Promise((resolve) => {
          fetchCallCount++;
          setTimeout(
            () =>
              resolve({
                ok: true,
                json: () =>
                  Promise.resolve({
                    accessToken: 'new-access',
                    user: { id: '1', email: 'a@b.com', displayName: 'Test' },
                  }),
              } as Response),
            10,
          );
        }),
    );

    const { attemptTokenRefresh } = await import('../token-refresh');

    const [r1, r2, r3] = await Promise.all([
      attemptTokenRefresh(),
      attemptTokenRefresh(),
      attemptTokenRefresh(),
    ]);

    expect(r1).not.toBeNull();
    expect(r2).not.toBeNull();
    expect(r3).not.toBeNull();
    expect(fetchCallCount).toBe(1);
  });
});
