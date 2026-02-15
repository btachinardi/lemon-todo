import { useState } from 'react';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { LoaderIcon, PlusIcon } from 'lucide-react';
import type { CreateTaskRequest } from '../../types/api.types';

interface QuickAddFormProps {
  /** Called with a minimal request containing only the trimmed title. */
  onSubmit: (request: CreateTaskRequest) => void;
  isLoading?: boolean;
  className?: string;
}

/**
 * Single-field form for rapid task creation. Clears the input on
 * successful submit. The submit button is disabled while empty or loading.
 */
export function QuickAddForm({ onSubmit, isLoading, className }: QuickAddFormProps) {
  const [title, setTitle] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const trimmed = title.trim();
    if (!trimmed) return;

    onSubmit({ title: trimmed });
    setTitle('');
  };

  return (
    <form onSubmit={handleSubmit} className={className}>
      <div className="flex gap-3">
        <label htmlFor="quick-add-input" className="sr-only">
          New task title
        </label>
        <Input
          id="quick-add-input"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="What needs to be done?"
          maxLength={500}
          disabled={isLoading}
          className="h-10 rounded-lg border-border bg-secondary/60 px-4 text-sm placeholder:text-muted-foreground/60 focus-visible:border-primary/40 focus-visible:bg-secondary/80"
        />
        <Button
          type="submit"
          size="sm"
          disabled={!title.trim() || isLoading}
          className="h-10 px-4 shadow-[0_0_16px_rgba(220,255,2,0.15)] transition-shadow hover:shadow-[0_0_24px_rgba(220,255,2,0.3)]"
        >
          {isLoading ? (
            <LoaderIcon className="size-4 animate-spin" />
          ) : (
            <PlusIcon className="size-4" />
          )}
          Add Task
        </Button>
      </div>
    </form>
  );
}
