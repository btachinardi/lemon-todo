import { useState, useRef, useCallback } from 'react';
import { format, parseISO } from 'date-fns';
import {
  CalendarIcon,
  Trash2Icon,
  XIcon,
  PlusIcon,
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
}: TaskDetailSheetProps) {
  const isOpen = taskId !== null;

  return (
    <Sheet open={isOpen} onOpenChange={(open) => !open && onClose()}>
      <SheetContent className="flex w-full flex-col gap-0 overflow-y-auto sm:max-w-lg">
        {taskId && (
          <TaskDetailContent
            task={task}
            isLoading={isLoading}
            isError={isError}
            onClose={onClose}
            onUpdateTitle={onUpdateTitle}
            onUpdateDescription={onUpdateDescription}
            onUpdatePriority={onUpdatePriority}
            onUpdateDueDate={onUpdateDueDate}
            onAddTag={onAddTag}
            onRemoveTag={onRemoveTag}
            onDelete={onDelete}
            isDeleting={isDeleting}
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
}: TaskDetailContentProps) {
  const [editingTitle, setEditingTitle] = useState(false);
  const [titleDraft, setTitleDraft] = useState(task?.title ?? '');
  const [descDraft, setDescDraft] = useState(task?.description ?? '');
  const [tagInput, setTagInput] = useState('');
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
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

  const handleDescSave = useCallback(() => {
    if (!task) return;
    const newDesc = descDraft.trim() || null;
    if (newDesc === (task.description ?? null)) return;
    onUpdateDescription(newDesc);
  }, [task, descDraft, onUpdateDescription]);

  const handleAddTag = useCallback(() => {
    if (!task) return;
    const tag = tagInput.trim();
    if (!tag) return;
    if (task.tags.includes(tag)) {
      setTagInput('');
      return;
    }
    onAddTag(tag);
    setTagInput('');
  }, [task, tagInput, onAddTag]);

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
        <p className="text-sm text-muted-foreground">Could not load task details.</p>
        <Button variant="outline" size="sm" onClick={onClose}>
          Close
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
            className="cursor-pointer text-lg font-semibold hover:text-primary transition-colors"
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
      </SheetHeader>

      <div className="flex flex-col gap-5 px-6 pb-6">
        {/* Description */}
        <div className="space-y-1.5">
          <Label htmlFor="task-description">Description</Label>
          <Textarea
            id="task-description"
            value={descDraft}
            onChange={(e) => setDescDraft(e.target.value)}
            onBlur={handleDescSave}
            placeholder="Add a description..."
            className="min-h-[80px] resize-none"
          />
        </div>

        {/* Priority */}
        <div className="space-y-1.5">
          <Label>Priority</Label>
          <Select value={task.priority} onValueChange={onUpdatePriority}>
            <SelectTrigger className="w-full" aria-label="Task priority">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {Object.values(Priority).map((p) => (
                <SelectItem key={p} value={p}>
                  {p === 'None' ? 'No priority' : p}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {/* Due Date */}
        <div className="space-y-1.5">
          <Label>Due Date</Label>
          <div className="flex items-center gap-2">
            <Popover>
              <PopoverTrigger asChild>
                <Button
                  variant="outline"
                  className={cn(
                    'w-full justify-start text-left font-normal',
                    !parsedDueDate && 'text-muted-foreground',
                  )}
                >
                  <CalendarIcon className="mr-2 size-4" />
                  {parsedDueDate ? format(parsedDueDate, 'PPP') : 'Pick a date'}
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
                aria-label="Clear due date"
              >
                <XIcon className="size-4" />
              </Button>
            )}
          </div>
        </div>

        {/* Tags */}
        <div className="space-y-1.5">
          <Label>Tags</Label>
          <div className="flex flex-wrap gap-1.5">
            {task.tags.map((tag) => (
              <Badge key={tag} variant="secondary" className="gap-1 pr-1">
                {tag}
                <button
                  onClick={() => onRemoveTag(tag)}
                  className="ml-0.5 rounded-full p-0.5 hover:bg-destructive/20 transition-colors"
                  aria-label={`Remove tag ${tag}`}
                >
                  <XIcon className="size-3" />
                </button>
              </Badge>
            ))}
          </div>
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
              placeholder="Add a tag..."
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
              Add
            </Button>
          </form>
        </div>

        {/* Delete */}
        <div className="border-t border-border/50 pt-4">
          {showDeleteConfirm ? (
            <div className="flex items-center gap-2">
              <p className="text-sm text-destructive">Delete this task?</p>
              <Button
                variant="destructive"
                size="sm"
                onClick={onDelete}
                disabled={isDeleting}
              >
                Confirm
              </Button>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => setShowDeleteConfirm(false)}
              >
                Cancel
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
              Delete task
            </Button>
          )}
        </div>
      </div>
    </>
  );
}
