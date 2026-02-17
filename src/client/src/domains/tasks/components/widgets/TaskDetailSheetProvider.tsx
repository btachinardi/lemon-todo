import { useState, useCallback, useMemo } from 'react';
import { format } from 'date-fns';
import { toastApiError } from '@/lib/toast-helpers';
import { useTaskQuery, useTasksQuery } from '../../hooks/use-tasks-query';
import { useUpdateTask, useDeleteTask, useAddTag, useRemoveTag } from '../../hooks/use-task-mutations';
import { useViewTaskNote } from '../../hooks/use-view-task-note';
import { useSaveIndicator } from '../../hooks/use-save-indicator';
import { TaskDetailSheet } from './TaskDetailSheet';
import { TaskNoteRevealDialog } from './TaskNoteRevealDialog';
import { useDevAccountPassword } from '@/domains/auth/hooks/use-dev-account-password';
import { useOfflineQueueStore } from '@/stores/use-offline-queue-store';

interface TaskDetailSheetProviderProps {
  taskId: string | null;
  onClose: () => void;
}

/**
 * Data-sourcing provider for {@link TaskDetailSheet}.
 * Fetches task data via TanStack Query and wires mutation hooks
 * to the pure presentational widget's callback props.
 */
export function TaskDetailSheetProvider({ taskId, onClose }: TaskDetailSheetProviderProps) {
  const taskQuery = useTaskQuery(taskId ?? '');
  const tasksQuery = useTasksQuery();
  const updateTask = useUpdateTask();
  const deleteTask = useDeleteTask();
  const addTag = useAddTag();
  const removeTag = useRemoveTag();
  const viewNote = useViewTaskNote();
  const devPassword = useDevAccountPassword();
  const pendingCount = useOfflineQueueStore((s) => s.pendingCount);
  const saveStatus = useSaveIndicator({
    isPending: updateTask.isPending,
    pendingCount,
  });
  const [noteDialogOpen, setNoteDialogOpen] = useState(false);

  const task = taskQuery.data;

  const allTags = useMemo(() => {
    if (!tasksQuery.data) return undefined;
    const tagSet = new Set<string>();
    for (const t of tasksQuery.data.items) {
      for (const tag of t.tags) tagSet.add(tag);
    }
    return [...tagSet].sort();
  }, [tasksQuery.data]);

  const handleUpdateTitle = useCallback(
    (title: string) => {
      if (!task) return;
      updateTask.mutate(
        { id: task.id, request: { title } },
        { onError: (e: Error) => toastApiError(e, 'Could not update title.') },
      );
    },
    [task, updateTask],
  );

  const handleUpdateDescription = useCallback(
    (description: string | null) => {
      if (!task) return;
      updateTask.mutate(
        { id: task.id, request: { description } },
        { onError: (e: Error) => toastApiError(e, 'Could not update description.') },
      );
    },
    [task, updateTask],
  );

  const handleUpdatePriority = useCallback(
    (priority: string) => {
      if (!task) return;
      updateTask.mutate(
        { id: task.id, request: { priority } },
        { onError: (e: Error) => toastApiError(e, 'Could not update priority.') },
      );
    },
    [task, updateTask],
  );

  const handleUpdateDueDate = useCallback(
    (date: Date | undefined) => {
      if (!task) return;
      if (date) {
        updateTask.mutate(
          { id: task.id, request: { dueDate: format(date, 'yyyy-MM-dd') } },
          { onError: (e: Error) => toastApiError(e, 'Could not update due date.') },
        );
      } else {
        updateTask.mutate(
          { id: task.id, request: { clearDueDate: true } },
          { onError: (e: Error) => toastApiError(e, 'Could not clear due date.') },
        );
      }
    },
    [task, updateTask],
  );

  const handleAddTag = useCallback(
    (tag: string) => {
      if (!task) return;
      addTag.mutate(
        { id: task.id, tag },
        { onError: (e: Error) => toastApiError(e, 'Could not add tag.') },
      );
    },
    [task, addTag],
  );

  const handleRemoveTag = useCallback(
    (tag: string) => {
      if (!task) return;
      removeTag.mutate(
        { id: task.id, tag },
        { onError: (e: Error) => toastApiError(e, 'Could not remove tag.') },
      );
    },
    [task, removeTag],
  );

  const handleDelete = useCallback(() => {
    if (!task) return;
    deleteTask.mutate(task.id, {
      onSuccess: () => onClose(),
      onError: (e: Error) => toastApiError(e, 'Could not delete task.'),
    });
  }, [task, deleteTask, onClose]);

  const handleUpdateSensitiveNote = useCallback(
    (note: string) => {
      if (!task) return;
      updateTask.mutate(
        { id: task.id, request: { sensitiveNote: note } },
        { onError: (e: Error) => toastApiError(e, 'Could not save note.') },
      );
    },
    [task, updateTask],
  );

  const handleClearSensitiveNote = useCallback(() => {
    if (!task) return;
    updateTask.mutate(
      { id: task.id, request: { clearSensitiveNote: true } },
      { onError: (e: Error) => toastApiError(e, 'Could not clear note.') },
    );
  }, [task, updateTask]);

  const handleViewNote = useCallback(() => {
    setNoteDialogOpen(true);
  }, []);

  const handleRevealNote = useCallback(
    (password: string) => {
      if (!task) return;
      viewNote.mutate({ id: task.id, password });
    },
    [task, viewNote],
  );

  const handleNoteDialogOpenChange = useCallback(
    (open: boolean) => {
      setNoteDialogOpen(open);
      if (!open) viewNote.reset();
    },
    [viewNote],
  );

  return (
    <>
      <TaskDetailSheet
        taskId={taskId}
        onClose={onClose}
        task={task}
        isLoading={taskQuery.isLoading}
        isError={taskQuery.isError}
        onUpdateTitle={handleUpdateTitle}
        onUpdateDescription={handleUpdateDescription}
        onUpdatePriority={handleUpdatePriority}
        onUpdateDueDate={handleUpdateDueDate}
        onAddTag={handleAddTag}
        onRemoveTag={handleRemoveTag}
        onDelete={handleDelete}
        isDeleting={deleteTask.isPending}
        allTags={allTags}
        onUpdateSensitiveNote={handleUpdateSensitiveNote}
        onClearSensitiveNote={handleClearSensitiveNote}
        onViewNote={handleViewNote}
        saveStatus={saveStatus}
      />
      <TaskNoteRevealDialog
        open={noteDialogOpen}
        onOpenChange={handleNoteDialogOpenChange}
        onReveal={handleRevealNote}
        isPending={viewNote.isPending}
        error={viewNote.error}
        revealedNote={viewNote.data?.note ?? null}
        devPassword={devPassword}
      />
    </>
  );
}
