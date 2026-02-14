import { useState } from 'react';
import { Button } from '@/ui/button';
import { Input } from '@/ui/input';
import { PlusIcon } from 'lucide-react';
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
        <Input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          placeholder="Add a task..."
          maxLength={500}
          disabled={isLoading}
          aria-label="New task title"
        />
        <Button type="submit" size="sm" disabled={!title.trim() || isLoading}>
          <PlusIcon className="size-4" />
          Add
        </Button>
      </div>
    </form>
  );
}
