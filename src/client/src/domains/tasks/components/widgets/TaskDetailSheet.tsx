import { useState, useRef, useCallback, useMemo, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { format, parseISO } from 'date-fns';
import {
  CalendarIcon,
  Trash2Icon,
  XIcon,
  PlusIcon,
  LockIcon,
  EyeIcon,
  LoaderCircleIcon,
  CheckCircle2Icon,
} from 'lucide-react';
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
} from '@/ui/sheet';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { Textarea } from '@/ui/textarea';
import { Label } from '@/ui/label';
import { Popover, PopoverContent, PopoverTrigger } from '@/ui/popover';
import { Calendar } from '@/ui/calendar';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/ui/select';
import { Badge } from '@/ui/badge';
import { Skeleton } from '@/ui/skeleton';
import { cn } from '@/lib/utils';
import type { Task } from '../../types/task.types';
import { Priority } from '../../types/task.types';

export interface TaskDetailSheetProps {
  taskId: string | null;
  onClose: () => void;
  /** The loaded task data. `undefined` while loading. */
  task: Task | undefined;
  /** Whether the task is currently loading. */
  isLoading: boolean;
  /** Whether the task query errored. */
  isError: boolean;
  /** Callbacks for task mutations. */
  onUpdateTitle: (title: string) => void;
  onUpdateDescription: (description: string | null) => void;
  onUpdatePriority: (priority: string) => void;
  onUpdateDueDate: (date: Date | undefined) => void;
  onAddTag: (tag: string) => void;
  onRemoveTag: (tag: string) => void;
  onDelete: () => void;
  isDeleting?: boolean;
  /** All distinct tags across all tasks, used for autocomplete suggestions. */
  allTags?: string[];
  /** Called when the user saves a new or replacement sensitive note. */
  onUpdateSensitiveNote?: (note: string) => void;
  /** Called when the user clears the encrypted sensitive note. */
  onClearSensitiveNote?: () => void;
  /** Called when the user wants to view the decrypted note. */
  onViewNote?: () => void;
  /** Mutation status for the update-task operation, drives the save indicator. */
  saveStatus?: 'idle' | 'pending' | 'success' | 'error';
}

/** Slide-over panel for viewing and editing all fields of a single task. */
export function TaskDetailSheet({
  taskId,
  onClose,
  task,
  isLoading,
  isError,
  onUpdateTitle,
  onUpdateDescription,
  onUpdatePriority,
  onUpdateDueDate,
  onAddTag,
  onRemoveTag,
  onDelete,
  isDeleting,
  allTags,
  onUpdateSensitiveNote,
  onClearSensitiveNote,
  onViewNote,
  saveStatus = 'idle',
}: TaskDetailSheetProps) {
  const isOpen = taskId !== null;

  const handleClose = useCallback(() => {
    // Force-blur the active element so any pending onBlur save handlers fire
    // before the sheet unmounts (e.g. description textarea).
    if (document.activeElement instanceof HTMLElement) {
      document.activeElement.blur();
    }
    onClose();
  }, [onClose]);

  return (
    <Sheet open={isOpen} onOpenChange={(open) => !open && handleClose()}>
      <SheetContent className="flex w-full flex-col gap-0 overflow-y-auto sm:max-w-lg">
        {taskId && (
          <TaskDetailContent
            task={task}
            isLoading={isLoading}
            isError={isError}
            onClose={handleClose}
            onUpdateTitle={onUpdateTitle}
            onUpdateDescription={onUpdateDescription}
            onUpdatePriority={onUpdatePriority}
            onUpdateDueDate={onUpdateDueDate}
            onAddTag={onAddTag}
            onRemoveTag={onRemoveTag}
            onDelete={onDelete}
            isDeleting={isDeleting}
            allTags={allTags}
            onUpdateSensitiveNote={onUpdateSensitiveNote}
            onClearSensitiveNote={onClearSensitiveNote}
            onViewNote={onViewNote}
            saveStatus={saveStatus}
          />
        )}
      </SheetContent>
    </Sheet>
  );
}

interface TaskDetailContentProps {
  task: Task | undefined;
  isLoading: boolean;
  isError: boolean;
  onClose: () => void;
  onUpdateTitle: (title: string) => void;
  onUpdateDescription: (description: string | null) => void;
  onUpdatePriority: (priority: string) => void;
  onUpdateDueDate: (date: Date | undefined) => void;
  onAddTag: (tag: string) => void;
  onRemoveTag: (tag: string) => void;
  onDelete: () => void;
  isDeleting?: boolean;
  allTags?: string[];
  onUpdateSensitiveNote?: (note: string) => void;
  onClearSensitiveNote?: () => void;
  onViewNote?: () => void;
  saveStatus: 'idle' | 'pending' | 'success' | 'error';
}

function TaskDetailContent({
  task,
  isLoading,
  isError,
  onClose,
  onUpdateTitle,
  onUpdateDescription,
  onUpdatePriority,
  onUpdateDueDate,
  onAddTag,
  onRemoveTag,
  onDelete,
  isDeleting,
  allTags,
  onUpdateSensitiveNote,
  onClearSensitiveNote,
  onViewNote,
  saveStatus,
}: TaskDetailContentProps) {
  const { t } = useTranslation();
  const [editingTitle, setEditingTitle] = useState(false);
  const [titleDraft, setTitleDraft] = useState(task?.title ?? '');
  const [descDraft, setDescDraft] = useState(task?.description ?? '');
  const [tagInput, setTagInput] = useState('');
  const [tagInputFocused, setTagInputFocused] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [noteDraft, setNoteDraft] = useState('');
  const titleInputRef = useRef<HTMLInputElement>(null);

  // Sync drafts when task data changes -- uses the React-approved "adjust
  // state during render" pattern instead of useEffect to avoid cascading renders.
  const [prevTitle, setPrevTitle] = useState(task?.title);
  const [prevDesc, setPrevDesc] = useState(task?.description);
  if (task && (task.title !== prevTitle || task.description !== prevDesc)) {
    setPrevTitle(task.title);
    setPrevDesc(task.description);
    setTitleDraft(task.title);
    setDescDraft(task.description ?? '');
  }

  const handleTitleSave = useCallback(() => {
    if (!task || titleDraft.trim() === task.title) {
      setEditingTitle(false);
      return;
    }
    const trimmed = titleDraft.trim();
    if (!trimmed) {
      setTitleDraft(task.title);
      setEditingTitle(false);
      return;
    }
    onUpdateTitle(trimmed);
    setEditingTitle(false);
  }, [task, titleDraft, onUpdateTitle]);

  // --- Debounced auto-save for description ---
  // Use refs so the flush function is stable and always reads the latest values.
  const descDraftRef = useRef(descDraft);
  descDraftRef.current = descDraft;
  const taskRef = useRef(task);
  taskRef.current = task;
  const onUpdateDescriptionRef = useRef(onUpdateDescription);
  onUpdateDescriptionRef.current = onUpdateDescription;

  // Track the last description value we've sent to the server, to avoid
  // duplicate saves when both the debounce timer and blur fire in sequence.
  const lastSavedDescRef = useRef(task?.description ?? null);

  // Update the "last saved" baseline when the server data changes
  // (e.g. after a mutation round-trip).
  if (task && (task.description ?? null) !== lastSavedDescRef.current) {
    lastSavedDescRef.current = task.description ?? null;
  }

  const descTimerRef = useRef<ReturnType<typeof setTimeout>>();

  // Stable flush function — cancels any pending debounce and saves immediately
  // if the current draft differs from the last saved value.
  const flushDescSave = useCallback(() => {
    clearTimeout(descTimerRef.current);
    const currentTask = taskRef.current;
    if (!currentTask) return;
    const newDesc = descDraftRef.current.trim() || null;
    if (newDesc === lastSavedDescRef.current) return;
    lastSavedDescRef.current = newDesc;
    onUpdateDescriptionRef.current(newDesc);
  }, []);

  // Auto-save description 1 second after the user stops typing.
  useEffect(() => {
    if (!task) return;
    const newDesc = descDraft.trim() || null;
    if (newDesc === lastSavedDescRef.current) return;

    descTimerRef.current = setTimeout(flushDescSave, 1000);
    return () => clearTimeout(descTimerRef.current);
  }, [descDraft, task, flushDescSave]);

  // Flush any pending debounced save on component unmount (e.g. route change).
  useEffect(() => {
    return () => flushDescSave();
  }, [flushDescSave]);

  // onBlur handler: immediately flush the debounce (same as pressing "save").
  const handleDescSave = flushDescSave;

  const handleAddTag = useCallback(() => {
    if (!task) return;
    const tag = tagInput.trim();
    if (!tag) return;
    const normalizedInput = tag.toLowerCase();
    if (task.tags.some((t) => t.toLowerCase() === normalizedInput)) {
      setTagInput('');
      return;
    }
    onAddTag(tag);
    setTagInput('');
  }, [task, tagInput, onAddTag]);

  const handleSaveNote = useCallback(() => {
    const trimmed = noteDraft.trim();
    if (!trimmed || !onUpdateSensitiveNote) return;
    onUpdateSensitiveNote(trimmed);
    setNoteDraft('');
  }, [noteDraft, onUpdateSensitiveNote]);

  const handleSelectSuggestion = useCallback(
    (tag: string) => {
      onAddTag(tag);
      setTagInput('');
    },
    [onAddTag],
  );

  const filteredSuggestions = useMemo(() => {
    if (!allTags || !task) return [];
    const taskTagsLower = new Set(task.tags.map((t) => t.toLowerCase()));
    const inputLower = tagInput.trim().toLowerCase();
    return allTags.filter((tag) => {
      if (taskTagsLower.has(tag.toLowerCase())) return false;
      if (inputLower && !tag.toLowerCase().includes(inputLower)) return false;
      return true;
    });
  }, [allTags, task, tagInput]);

  if (isLoading) {
    return (
      <div className="flex flex-col gap-6 p-6" data-testid="task-detail-skeleton">
        <Skeleton className="h-8 w-3/4" />
        <Skeleton className="h-20 w-full" />
        <Skeleton className="h-10 w-1/2" />
        <Skeleton className="h-10 w-1/2" />
      </div>
    );
  }

  if (isError || !task) {
    return (
      <div className="flex flex-col items-center gap-4 p-6">
        <p className="text-sm text-muted-foreground">{t('tasks.detail.loadError')}</p>
        <Button variant="outline" size="sm" onClick={onClose}>
          {t('common.close')}
        </Button>
      </div>
    );
  }

  const parsedDueDate = task.dueDate ? parseISO(task.dueDate) : undefined;

  return (
    <>
      <SheetHeader className="px-6 pt-6 pb-4">
        {editingTitle ? (
          <Input
            ref={titleInputRef}
            value={titleDraft}
            onChange={(e) => setTitleDraft(e.target.value)}
            onBlur={handleTitleSave}
            onKeyDown={(e) => {
              if (e.key === 'Enter') handleTitleSave();
              if (e.key === 'Escape') {
                setTitleDraft(task.title);
                setEditingTitle(false);
              }
            }}
            className="text-lg font-semibold"
            aria-label="Task title"
            autoFocus
          />
        ) : (
          <SheetTitle
            className="cursor-pointer text-lg font-semibold hover:text-lemon transition-colors"
            onClick={() => {
              setEditingTitle(true);
              setTimeout(() => titleInputRef.current?.focus(), 0);
            }}
            role="button"
            tabIndex={0}
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                setEditingTitle(true);
              }
            }}
          >
            {task.title}
          </SheetTitle>
        )}
        <SheetDescription className="sr-only">Edit task details</SheetDescription>
        {saveStatus === 'pending' && (
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground" aria-live="polite">
            <LoaderCircleIcon className="size-3 animate-spin" />
            {t('tasks.detail.saving')}
          </p>
        )}
        {saveStatus === 'success' && (
          <p className="flex items-center gap-1.5 text-xs text-muted-foreground" aria-live="polite">
            <CheckCircle2Icon className="size-3 text-green-600 dark:text-green-400" />
            {t('tasks.detail.saved')}
          </p>
        )}
      </SheetHeader>

      <div className="flex flex-col gap-5 px-6 pb-6">
        {/* Description */}
        <div className="space-y-1.5">
          <Label htmlFor="task-description">{t('tasks.detail.description')}</Label>
          <Textarea
            id="task-description"
            value={descDraft}
            onChange={(e) => setDescDraft(e.target.value)}
            onBlur={handleDescSave}
            placeholder={t('tasks.detail.descriptionPlaceholder')}
            className="min-h-[80px] resize-none"
          />
        </div>

        {/* Priority */}
        <div className="space-y-1.5">
          <Label>{t('tasks.detail.priority')}</Label>
          <Select value={task.priority} onValueChange={onUpdatePriority}>
            <SelectTrigger className="w-full" aria-label="Task priority">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {Object.values(Priority).map((p) => (
                <SelectItem key={p} value={p}>
                  {p === 'None' ? t('tasks.detail.noPriority') : t(`tasks.priority.${p.toLowerCase()}`)}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Due Date */}
        <div className="space-y-1.5">
          <Label>{t('tasks.detail.dueDate')}</Label>
          <div className="flex items-center gap-2">
            <Popover>
              <PopoverTrigger asChild>
                <Button
                  variant="outline"
                  className={cn(
                    'flex-1 min-w-0 justify-start truncate text-left font-normal',
                    !parsedDueDate && 'text-muted-foreground',
                  )}
                >
                  <CalendarIcon className="mr-2 size-4" />
                  {parsedDueDate ? format(parsedDueDate, 'PPP') : t('tasks.detail.pickDate')}
                </Button>
              </PopoverTrigger>
              <PopoverContent className="w-auto p-0" align="start">
                <Calendar
                  mode="single"
                  selected={parsedDueDate}
                  onSelect={onUpdateDueDate}
                  initialFocus
                />
              </PopoverContent>
            </Popover>
            {parsedDueDate && (
              <Button
                variant="ghost"
                size="icon"
                className="size-8 shrink-0"
                onClick={() => onUpdateDueDate(undefined)}
                aria-label={t('tasks.detail.clearDueDate')}
              >
                <XIcon className="size-4" />
              </Button>
            )}
          </div>
        </div>

        {/* Tags */}
        <div className="space-y-1.5">
          <Label>{t('tasks.detail.tags')}</Label>
          <div className="flex flex-wrap gap-1.5">
            {task.tags.map((tag) => (
              <Badge key={tag} variant="secondary" className="gap-1 pr-1">
                {tag}
                <button
                  onClick={() => onRemoveTag(tag)}
                  className="ml-0.5 rounded-full p-0.5 hover:bg-destructive/20 transition-colors"
                  aria-label={t('tasks.detail.removeTag', { tag })}
                >
                  <XIcon className="size-3" />
                </button>
              </Badge>
            ))}
          </div>
          <div className="relative">
            <form
              className="flex gap-2"
              onSubmit={(e) => {
                e.preventDefault();
                handleAddTag();
              }}
            >
              <Input
                value={tagInput}
                onChange={(e) => setTagInput(e.target.value)}
                onFocus={() => setTagInputFocused(true)}
                onBlur={() => {
                  // Delay hiding so click on suggestion registers first
                  setTimeout(() => setTagInputFocused(false), 150);
                }}
                placeholder={t('tasks.detail.tagPlaceholder')}
                className="h-8 text-sm"
                aria-label="New tag"
              />
              <Button
                type="submit"
                variant="outline"
                size="sm"
                className="h-8 shrink-0"
                disabled={!tagInput.trim()}
              >
                <PlusIcon className="mr-1 size-3" />
                {t('common.add')}
              </Button>
            </form>
            {tagInputFocused && filteredSuggestions.length > 0 && (
              <ul
                className="absolute z-50 mt-1 max-h-32 w-full overflow-y-auto rounded-md border bg-popover p-1 shadow-md"
                role="listbox"
              >
                {filteredSuggestions.map((tag) => (
                  <li
                    key={tag}
                    role="option"
                    aria-selected={false}
                    className="cursor-pointer rounded-sm px-2 py-1 text-sm hover:bg-accent hover:text-accent-foreground"
                    onMouseDown={(e) => {
                      e.preventDefault(); // Prevent blur before click
                      handleSelectSuggestion(tag);
                    }}
                  >
                    {tag}
                  </li>
                ))}
              </ul>
            )}
          </div>
        </div>

        {/* Sensitive Note */}
        <div className="space-y-2">
          <Label className="flex items-center gap-1.5">
            <LockIcon className="size-3.5 text-amber-500" />
            {t('tasks.sensitiveNote.label')}
          </Label>

          {task.sensitiveNote && (
            <div className="flex items-center gap-2 rounded-md border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950">
              <span className="text-sm text-amber-700 dark:text-amber-300">
                {t('tasks.sensitiveNote.hasNote')}
              </span>
              <Button
                variant="outline"
                size="sm"
                className="ml-auto gap-1.5"
                onClick={onViewNote}
              >
                <EyeIcon className="size-3.5" />
                {t('tasks.sensitiveNote.viewNote')}
              </Button>
            </div>
          )}

          <Textarea
            value={noteDraft}
            onChange={(e) => setNoteDraft(e.target.value)}
            placeholder={t('tasks.sensitiveNote.placeholder')}
            className="min-h-[60px] resize-none"
          />
          <p className="text-xs text-muted-foreground">
            {t('tasks.sensitiveNote.encryptionWarning')}
          </p>

          <div className="flex gap-2">
            {noteDraft.trim() && (
              <Button size="sm" onClick={handleSaveNote}>
                {t('tasks.sensitiveNote.save')}
              </Button>
            )}
            {task.sensitiveNote && onClearSensitiveNote && (
              <Button
                variant="ghost"
                size="sm"
                className="text-destructive hover:text-destructive hover:bg-destructive/10"
                onClick={onClearSensitiveNote}
              >
                {t('common.clear')}
              </Button>
            )}
          </div>
        </div>

        {/* Delete */}
        <div className="border-t border-border/50 pt-4">
          {showDeleteConfirm ? (
            <div className="flex items-center gap-2">
              <p className="text-sm text-destructive">{t('tasks.detail.deleteTitle')}</p>
              <Button
                variant="destructive"
                size="sm"
                onClick={onDelete}
                disabled={isDeleting}
              >
                {t('common.confirm')}
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setShowDeleteConfirm(false)}
              >
                {t('common.cancel')}
              </Button>
            </div>
          ) : (
            <Button
              variant="ghost"
              size="sm"
              className="text-destructive hover:text-destructive hover:bg-destructive/10"
              onClick={() => setShowDeleteConfirm(true)}
            >
              <Trash2Icon className="mr-1.5 size-4" />
              {t('tasks.detail.deleteButton')}
            </Button>
          )}
        </div>
      </div>
    </>
  );
}
