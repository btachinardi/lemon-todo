import { useState } from 'react';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { LoaderIcon, PlusIcon } from 'lucide-react';
import type { CreateTaskRequest } from '../../types/api.types';

interface QuickAddFormProps {
  onSubmit: (request: CreateTaskRequest) => void;
  isLoading?: boolean;
  className?: string;
}

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
      <div className="flex gap-2">
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
        />
        <Button type="submit" size="sm" disabled={!title.trim() || isLoading}>
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
