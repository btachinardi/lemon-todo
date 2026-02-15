import { useMutation, useQueryClient } from '@tanstack/react-query';
import { tasksApi } from '../api/tasks.api';
import type { CreateTaskRequest, MoveTaskRequest, UpdateTaskRequest } from '../types/api.types';
import { boardKeys } from './use-board-query';
import { taskKeys } from './use-tasks-query';

/**
 * Invalidates both task and board query caches.
 * Required because board cards change when task lifecycle mutations fire
 * (e.g. completing a task moves its card to the Done column).
 */
function invalidateTaskAndBoard(queryClient: ReturnType<typeof useQueryClient>) {
  queryClient.invalidateQueries({ queryKey: taskKeys.all });
  queryClient.invalidateQueries({ queryKey: boardKeys.all });
}

/** Creates a new task and places it on the default board. */
export function useCreateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: CreateTaskRequest) => tasksApi.create(request),
    onSuccess: () => invalidateTaskAndBoard(queryClient),
  });
}

/** Partial-updates a task. Only invalidates task cache (no board impact). */
export function useUpdateTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateTaskRequest }) =>
      tasksApi.update(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}

/** Transitions a task to `Done`. Invalidates both task and board caches. */
export function useCompleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tasksApi.complete(id),
    onSuccess: () => invalidateTaskAndBoard(queryClient),
  });
}

/** Reverts a completed task to its previous status. */
export function useUncompleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tasksApi.uncomplete(id),
    onSuccess: () => invalidateTaskAndBoard(queryClient),
  });
}

/** Soft-deletes a task and removes its board card. */
export function useDeleteTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => tasksApi.delete(id),
    onSuccess: () => invalidateTaskAndBoard(queryClient),
  });
}

/** Moves a task card to a different column/position on the board. */
export function useMoveTask() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: MoveTaskRequest }) =>
      tasksApi.move(id, request),
    onSuccess: () => invalidateTaskAndBoard(queryClient),
  });
}

/** Adds a tag to a task. Only invalidates task cache. */
export function useAddTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, tag }: { id: string; tag: string }) =>
      tasksApi.addTag(id, { tag }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}

/** Removes a tag from a task. Only invalidates task cache. */
export function useRemoveTag() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, tag }: { id: string; tag: string }) =>
      tasksApi.removeTag(id, tag),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: taskKeys.all });
    },
  });
}
