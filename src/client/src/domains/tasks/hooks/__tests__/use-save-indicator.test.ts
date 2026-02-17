import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { useSaveIndicator } from '../use-save-indicator';

describe('useSaveIndicator', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('should return idle initially', () => {
    const { result } = renderHook(() => useSaveIndicator(false, false, 'task-1'));
    expect(result.current).toBe('idle');
  });

  it('should return pending when isPending is true', () => {
    const { result } = renderHook(() => useSaveIndicator(true, false, 'task-1'));
    expect(result.current).toBe('pending');
  });

  it('should return success when isSuccess is true', () => {
    const { result } = renderHook(() => useSaveIndicator(false, true, 'task-1'));
    expect(result.current).toBe('success');
  });

  it('should auto-dismiss success to idle after 2 seconds', () => {
    const { result } = renderHook(
      ({ isPending, isSuccess, taskId }) => useSaveIndicator(isPending, isSuccess, taskId),
      { initialProps: { isPending: false, isSuccess: true, taskId: 'task-1' } },
    );

    expect(result.current).toBe('success');

    act(() => vi.advanceTimersByTime(2000));

    expect(result.current).toBe('idle');
  });

  it('should reset to idle when taskId changes', () => {
    const { result, rerender } = renderHook(
      ({ isPending, isSuccess, taskId }) => useSaveIndicator(isPending, isSuccess, taskId),
      { initialProps: { isPending: false, isSuccess: true, taskId: 'task-1' } },
    );

    expect(result.current).toBe('success');

    // Switch to a different task
    rerender({ isPending: false, isSuccess: true, taskId: 'task-2' });

    expect(result.current).toBe('idle');
  });

  it('should show pending again when a new mutation starts after success', () => {
    const { result, rerender } = renderHook(
      ({ isPending, isSuccess, taskId }) => useSaveIndicator(isPending, isSuccess, taskId),
      { initialProps: { isPending: false, isSuccess: true, taskId: 'task-1' } },
    );

    expect(result.current).toBe('success');

    // New mutation starts
    rerender({ isPending: true, isSuccess: false, taskId: 'task-1' });

    expect(result.current).toBe('pending');
  });

  it('should cancel auto-dismiss timer when a new mutation starts during success period', () => {
    const { result, rerender } = renderHook(
      ({ isPending, isSuccess, taskId }) => useSaveIndicator(isPending, isSuccess, taskId),
      { initialProps: { isPending: false, isSuccess: true, taskId: 'task-1' } },
    );

    expect(result.current).toBe('success');

    // Advance only 1 second (less than the 2s dismiss delay)
    act(() => vi.advanceTimersByTime(1000));

    // New mutation starts before auto-dismiss fires
    rerender({ isPending: true, isSuccess: false, taskId: 'task-1' });

    expect(result.current).toBe('pending');

    // Advance past the original 2s mark — should not auto-dismiss to idle
    act(() => vi.advanceTimersByTime(1500));

    // Still pending (the old timer was cancelled)
    expect(result.current).toBe('pending');
  });
});
