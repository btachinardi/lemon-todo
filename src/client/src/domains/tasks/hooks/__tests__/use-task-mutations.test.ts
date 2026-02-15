import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import {
  useCreateTask,
  useCompleteTask,
  useUncompleteTask,
  useDeleteTask,
  useAddTag,
  useRemoveTag,
} from '../use-task-mutations';

// Mock the toast helpers module
const mockToastSuccess = vi.fn();
vi.mock('@/lib/toast-helpers', () => ({
  toastSuccess: (...args: unknown[]) => mockToastSuccess(...args),
  toastApiError: vi.fn(),
}));

// Mock the tasks API module
vi.mock('../../api/tasks.api', () => ({
  tasksApi: {
    create: vi.fn().mockResolvedValue({ id: '1', title: 'Test' }),
    complete: vi.fn().mockResolvedValue(undefined),
    uncomplete: vi.fn().mockResolvedValue(undefined),
    delete: vi.fn().mockResolvedValue(undefined),
    addTag: vi.fn().mockResolvedValue(undefined),
    removeTag: vi.fn().mockResolvedValue(undefined),
    move: vi.fn().mockResolvedValue(undefined),
    update: vi.fn().mockResolvedValue({ id: '1', title: 'Updated' }),
  },
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

describe('use-task-mutations toasts', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('should toast "Task created" on successful create', async () => {
    const { result } = renderHook(() => useCreateTask(), { wrapper: createWrapper() });

    result.current.mutate({ title: 'New task' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockToastSuccess).toHaveBeenCalledWith('Task created');
  });

  it('should toast "Task completed" on successful complete', async () => {
    const { result } = renderHook(() => useCompleteTask(), { wrapper: createWrapper() });

    result.current.mutate('task-1');

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockToastSuccess).toHaveBeenCalledWith('Task completed');
  });

  it('should toast "Task reopened" on successful uncomplete', async () => {
    const { result } = renderHook(() => useUncompleteTask(), { wrapper: createWrapper() });

    result.current.mutate('task-1');

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockToastSuccess).toHaveBeenCalledWith('Task reopened');
  });

  it('should toast "Task deleted" on successful delete', async () => {
    const { result } = renderHook(() => useDeleteTask(), { wrapper: createWrapper() });

    result.current.mutate('task-1');

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockToastSuccess).toHaveBeenCalledWith('Task deleted');
  });

  it('should toast "Tag added" on successful addTag', async () => {
    const { result } = renderHook(() => useAddTag(), { wrapper: createWrapper() });

    result.current.mutate({ id: 'task-1', tag: 'urgent' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockToastSuccess).toHaveBeenCalledWith('Tag added');
  });

  it('should toast "Tag removed" on successful removeTag', async () => {
    const { result } = renderHook(() => useRemoveTag(), { wrapper: createWrapper() });

    result.current.mutate({ id: 'task-1', tag: 'urgent' });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(mockToastSuccess).toHaveBeenCalledWith('Tag removed');
  });
});
