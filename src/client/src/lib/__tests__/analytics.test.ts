/**
 * @vitest-environment jsdom
 */
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';

describe('analytics flush', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
    vi.resetModules();
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
  });

  it('should include Authorization header when access token is available', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: true,
    } as Response);

    // Dynamic import so both analytics and auth store share the same module graph
    const { useAuthStore } = await import('@/domains/auth/stores/use-auth-store');
    useAuthStore.getState().setAuth('test-token-123', {
      id: '1',
      email: 'a@b.com',
      displayName: 'Test',
      roles: [],
    });

    const { track, initAnalytics, stopAnalytics } = await import('../analytics');
    initAnalytics();

    await track('test_event');

    await vi.advanceTimersByTimeAsync(30_000);

    stopAnalytics();

    expect(fetchSpy).toHaveBeenCalledTimes(1);
    const [, options] = fetchSpy.mock.calls[0];
    expect((options as RequestInit).headers).toHaveProperty(
      'Authorization',
      'Bearer test-token-123',
    );
  });

  it('should not call fetch when no access token is available', async () => {
    const fetchSpy = vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: true,
    } as Response);

    const { track, initAnalytics, stopAnalytics } = await import('../analytics');
    initAnalytics();

    await track('test_event');

    await vi.advanceTimersByTimeAsync(30_000);

    stopAnalytics();

    expect(fetchSpy).not.toHaveBeenCalled();
  });

  it('should preserve buffered events when flush is skipped due to missing token', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValueOnce({
      ok: true,
    } as Response);

    const { track, initAnalytics, stopAnalytics, getBufferSize } = await import('../analytics');
    initAnalytics();

    await track('event_1');
    await track('event_2');

    expect(getBufferSize()).toBe(2);

    await vi.advanceTimersByTimeAsync(30_000);

    stopAnalytics();

    // Events should still be in buffer since flush was skipped
    expect(getBufferSize()).toBe(2);
  });
});
