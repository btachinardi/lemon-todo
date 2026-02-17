import { useMutation, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { tasksApi } from '../api/tasks.api';
import type { CreateTaskRequest, MoveTaskRequest, UpdateTaskRequest } from '../types/api.types';
import { boardKeys } from './use-board-query';
import { taskKeys } from './use-tasks-query';
import { track } from '@/lib/analytics';
import { toastSuccess } from '@/lib/toast-helpers';
import { useOfflineQueueStore } from '@/stores/use-offline-queue-store';

/**
 * Invalidates both task and board query caches.
 * Required because board cards change when task lifecycle mutations fire
 * (e.g. completing a task moves its card to the Done column).
 */
function invalidateTaskAndBoard(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: taskKeys.all });
  queryClient.invalidateQueries({ queryKey: boardKeys.all });
}

/**
 * Returns true if the error is a network failure that should be queued
 * for offline replay (TypeError from fetch when offline).
 */
function isOfflineNetworkError(error: unknown): boolean {
  return !navigator.onLine && error instanceof TypeError;
}

/**
 * Enqueues a mutation to the offline queue and shows an info toast.
 */
async function enqueueOffline(
  method: 'POST' | 'PUT' | 'DELETE',
  url: string,
  body?: unknown,
): Promise<void> {
  await useOfflineQueueStore.getState().enqueueMutation(method, url, body);
  toast.info('Change saved offline. Will sync when connected.');
}

/** Creates a new task and places it on the default board. */
export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (request: CreateTaskRequest) => {
      try {
        return await tasksApi.create(request);
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('POST', '/api/tasks', request);
          return undefined as never;
        }
        throw error;
      }
    },
    onSuccess: () => {
      invalidateTaskAndBoard(queryClient);
      toastSuccess('Task created');
      track('task_created_ui');
    },
  });
}

/** Partial-updates a task. Only invalidates task cache (no board impact). */
export function useUpdateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: UpdateTaskRequest }) => {
      try {
        return await tasksApi.update(id, request);
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('PUT', `/api/tasks/${id}`, request);
          return undefined as never;
        }
        throw error;
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}

/** Transitions a task to `Done`. Invalidates both task and board caches. */
export function useCompleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        return await tasksApi.complete(id);
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('POST', `/api/tasks/${id}/complete`);
          return;
        }
        throw error;
      }
    },
    onSuccess: () => {
      invalidateTaskAndBoard(queryClient);
      toastSuccess('Task completed');
      track('task_completed_ui');
    },
  });
}

/** Reverts a completed task to its previous status. */
export function useUncompleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        return await tasksApi.uncomplete(id);
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('POST', `/api/tasks/${id}/uncomplete`);
          return;
        }
        throw error;
      }
    },
    onSuccess: () => {
      invalidateTaskAndBoard(queryClient);
      toastSuccess('Task reopened');
    },
  });
}

/** Soft-deletes a task and removes its board card. */
export function useDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      try {
        return await tasksApi.delete(id);
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('DELETE', `/api/tasks/${id}`);
          return;
        }
        throw error;
      }
    },
    onSuccess: () => {
      invalidateTaskAndBoard(queryClient);
      toastSuccess('Task deleted');
      track('task_deleted_ui');
    },
  });
}

/** Moves a task card to a different column/position on the board. */
export function useMoveTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, request }: { id: string; request: MoveTaskRequest }) => {
      try {
        return await tasksApi.move(id, request);
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('POST', `/api/tasks/${id}/move`, request);
          return;
        }
        throw error;
      }
    },
    onSuccess: () => {
      invalidateTaskAndBoard(queryClient);
      track('task_moved_ui');
    },
  });
}

/** Adds a tag to a task. Only invalidates task cache. */
export function useAddTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, tag }: { id: string; tag: string }) => {
      try {
        return await tasksApi.addTag(id, { tag });
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('POST', `/api/tasks/${id}/tags`, { tag });
          return;
        }
        throw error;
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
      toastSuccess('Tag added');
    },
  });
}

/** Removes a tag from a task. Only invalidates task cache. */
export function useRemoveTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, tag }: { id: string; tag: string }) => {
      try {
        return await tasksApi.removeTag(id, tag);
      } catch (error) {
        if (isOfflineNetworkError(error)) {
          await enqueueOffline('DELETE', `/api/tasks/${id}/tags/${encodeURIComponent(tag)}`);
          return;
        }
        throw error;
      }
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
      toastSuccess('Tag removed');
    },
  });
}
