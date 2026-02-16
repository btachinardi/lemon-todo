import { useMutation } from '@tanstack/react-query';
import { tasksApi } from '../api/tasks.api';

/** Decrypts and returns a task's sensitive note after password re-authentication. */
export function useViewTaskNote() {
  return useMutation({
    mutationFn: ({ id, password }: { id: string; password: string }) =>
      tasksApi.viewNote(id, { password }),
  });
}
