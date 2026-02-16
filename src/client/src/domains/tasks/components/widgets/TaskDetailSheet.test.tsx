import { describe, it, expect, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { TaskDetailSheet } from './TaskDetailSheet';
import { createTask } from '@/test/factories';
import { Priority, TaskStatus } from '../../types/task.types';

const testTask = createTask({
  id: 'task-1',
  title: 'Buy groceries',
  description: 'Milk, eggs, bread',
  priority: Priority.Medium,
  status: TaskStatus.Todo,
  dueDate: '2026-03-15',
  tags: ['shopping', 'urgent'],
});

function renderSheet(overrides: Partial<React.ComponentProps<typeof TaskDetailSheet>> = {}) {
  const defaultProps: React.ComponentProps<typeof TaskDetailSheet> = {
    taskId: 'task-1',
    onClose: vi.fn(),
    task: testTask,
    isLoading: false,
    isError: false,
    onUpdateTitle: vi.fn(),
    onUpdateDescription: vi.fn(),
    onUpdatePriority: vi.fn(),
    onUpdateDueDate: vi.fn(),
    onAddTag: vi.fn(),
    onRemoveTag: vi.fn(),
    onDelete: vi.fn(),
    isDeleting: false,
    saveStatus: 'idle',
    ...overrides,
  };
  return { ...render(<TaskDetailSheet {...defaultProps} />), props: defaultProps };
}

describe('TaskDetailSheet', () => {
  it('should not render content when taskId is null', () => {
    renderSheet({ taskId: null });
    expect(screen.queryByText('Buy groceries')).not.toBeInTheDocument();
  });

  it('should show skeleton while loading', () => {
    renderSheet({ isLoading: true, task: undefined });
    expect(screen.getByTestId('task-detail-skeleton')).toBeInTheDocument();
  });

  it('should render task title when loaded', () => {
    renderSheet();
    expect(screen.getByText('Buy groceries')).toBeInTheDocument();
  });

  it('should render task description', () => {
    renderSheet();
    expect(screen.getByDisplayValue('Milk, eggs, bread')).toBeInTheDocument();
  });

  it('should render tags', () => {
    renderSheet();
    expect(screen.getByText('shopping')).toBeInTheDocument();
    expect(screen.getByText('urgent')).toBeInTheDocument();
  });

  it('should call onUpdateTitle on title blur', async () => {
    const onUpdateTitle = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onUpdateTitle });

    // Click the title to enter edit mode
    await user.click(screen.getByText('Buy groceries'));
    const titleInput = screen.getByLabelText('Task title');
    await user.clear(titleInput);
    await user.type(titleInput, 'Buy organic groceries');
    await user.tab(); // blur

    await waitFor(() =>
      expect(onUpdateTitle).toHaveBeenCalledWith('Buy organic groceries'),
    );
  });

  it('should render priority selector with current value', () => {
    renderSheet();
    expect(screen.getByLabelText('Task priority')).toBeInTheDocument();
  });

  it('should show delete confirmation on delete click', async () => {
    const user = userEvent.setup();
    renderSheet();

    await user.click(screen.getByText('Delete task'));
    expect(screen.getByText('Delete this task?')).toBeInTheDocument();
    expect(screen.getByText('Confirm')).toBeInTheDocument();
  });

  it('should call onDelete on confirm', async () => {
    const onDelete = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onDelete });

    await user.click(screen.getByText('Delete task'));
    await user.click(screen.getByText('Confirm'));

    expect(onDelete).toHaveBeenCalledOnce();
  });

  it('should call onAddTag when adding a tag', async () => {
    const onAddTag = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onAddTag });

    const tagInput = screen.getByLabelText('New tag');
    await user.type(tagInput, 'groceries');
    await user.click(screen.getByRole('button', { name: /add/i }));

    expect(onAddTag).toHaveBeenCalledWith('groceries');
  });

  it('should call onRemoveTag when removing a tag', async () => {
    const onRemoveTag = vi.fn();
    const user = userEvent.setup();
    renderSheet({ onRemoveTag });

    await user.click(screen.getByLabelText('Remove tag shopping'));

    expect(onRemoveTag).toHaveBeenCalledWith('shopping');
  });

  it('should show error state when isError is true', () => {
    renderSheet({ isError: true, task: undefined });
    expect(screen.getByText('Could not load task details.')).toBeInTheDocument();
  });

  it('should use flex-1 instead of w-full on due date trigger so clear button does not cause overflow', () => {
    renderSheet({
      task: createTask({ dueDate: '2026-03-15' }),
    });

    // The due date trigger button should use flex-1 (not w-full) to share
    // space with the clear button without overflowing the container.
    const dueDateTrigger = screen.getByRole('button', { name: /march 15/i });
    expect(dueDateTrigger.className).toContain('flex-1');
    expect(dueDateTrigger.className).not.toContain('w-full');
  });

  describe('sensitive note', () => {
    it('should show encrypted note indicator when task has a sensitive note', () => {
      renderSheet({
        task: createTask({ sensitiveNote: '[PROTECTED]' }),
      });
      expect(screen.getByText('Has encrypted note')).toBeInTheDocument();
    });

    it('should show View Note button when task has a sensitive note', () => {
      renderSheet({
        task: createTask({ sensitiveNote: '[PROTECTED]' }),
        onViewNote: vi.fn(),
      });
      expect(screen.getByRole('button', { name: /view note/i })).toBeInTheDocument();
    });

    it('should call onViewNote when View Note button is clicked', async () => {
      const onViewNote = vi.fn();
      const user = userEvent.setup();
      renderSheet({
        task: createTask({ sensitiveNote: '[PROTECTED]' }),
        onViewNote,
      });

      await user.click(screen.getByRole('button', { name: /view note/i }));
      expect(onViewNote).toHaveBeenCalledOnce();
    });

    it('should not show encrypted note indicator when no sensitive note', () => {
      renderSheet({
        task: createTask({ sensitiveNote: null }),
      });
      expect(screen.queryByText('Has encrypted note')).not.toBeInTheDocument();
    });

    it('should show Save Note button after typing in sensitive note textarea', async () => {
      const user = userEvent.setup();
      renderSheet();

      const textarea = screen.getByPlaceholderText('Enter sensitive information (encrypted at rest)...');
      await user.type(textarea, 'My secret data');

      expect(screen.getByRole('button', { name: /save note/i })).toBeInTheDocument();
    });

    it('should call onUpdateSensitiveNote when Save Note is clicked', async () => {
      const onUpdateSensitiveNote = vi.fn();
      const user = userEvent.setup();
      renderSheet({ onUpdateSensitiveNote });

      const textarea = screen.getByPlaceholderText('Enter sensitive information (encrypted at rest)...');
      await user.type(textarea, 'My secret data');
      await user.click(screen.getByRole('button', { name: /save note/i }));

      expect(onUpdateSensitiveNote).toHaveBeenCalledWith('My secret data');
    });

    it('should call onClearSensitiveNote when Clear button is clicked', async () => {
      const onClearSensitiveNote = vi.fn();
      const user = userEvent.setup();
      renderSheet({
        task: createTask({ sensitiveNote: '[PROTECTED]' }),
        onClearSensitiveNote,
      });

      await user.click(screen.getByRole('button', { name: /^clear$/i }));
      expect(onClearSensitiveNote).toHaveBeenCalledOnce();
    });

    it('should show encryption warning text', () => {
      renderSheet();
      expect(screen.getByText('This content is encrypted and can only be viewed with your password.')).toBeInTheDocument();
    });
  });

  describe('tag suggestions', () => {
    const allTags = ['shopping', 'urgent', 'work', 'personal', 'errands'];

    it('should show suggestion list when tag input is focused and allTags is provided', async () => {
      const user = userEvent.setup();
      renderSheet({ allTags });

      const tagInput = screen.getByLabelText('New tag');
      await user.click(tagInput);

      // Should show tags that are NOT already on the task (task has 'shopping' and 'urgent')
      expect(screen.getByRole('option', { name: 'work' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'personal' })).toBeInTheDocument();
      expect(screen.getByRole('option', { name: 'errands' })).toBeInTheDocument();
    });

    it('should not show tags already on the task in suggestions', async () => {
      const user = userEvent.setup();
      renderSheet({ allTags });

      const tagInput = screen.getByLabelText('New tag');
      await user.click(tagInput);

      // 'shopping' and 'urgent' are already on testTask
      const options = screen.getAllByRole('option');
      const optionTexts = options.map((o) => o.textContent);
      expect(optionTexts).not.toContain('shopping');
      expect(optionTexts).not.toContain('urgent');
    });

    it('should filter suggestions as user types', async () => {
      const user = userEvent.setup();
      renderSheet({ allTags });

      const tagInput = screen.getByLabelText('New tag');
      await user.type(tagInput, 'wo');

      expect(screen.getByRole('option', { name: 'work' })).toBeInTheDocument();
      expect(screen.queryByRole('option', { name: 'personal' })).not.toBeInTheDocument();
      expect(screen.queryByRole('option', { name: 'errands' })).not.toBeInTheDocument();
    });

    it('should call onAddTag with suggestion value when clicking a suggestion', async () => {
      const onAddTag = vi.fn();
      const user = userEvent.setup();
      renderSheet({ onAddTag, allTags });

      const tagInput = screen.getByLabelText('New tag');
      await user.click(tagInput);
      await user.click(screen.getByRole('option', { name: 'work' }));

      expect(onAddTag).toHaveBeenCalledWith('work');
    });

    it('should clear input after selecting a suggestion', async () => {
      const user = userEvent.setup();
      renderSheet({ allTags });

      const tagInput = screen.getByLabelText('New tag');
      await user.type(tagInput, 'wo');
      await user.click(screen.getByRole('option', { name: 'work' }));

      expect(tagInput).toHaveValue('');
    });

    it('should not call onAddTag for case-different duplicate of tag already on task', async () => {
      const onAddTag = vi.fn();
      const user = userEvent.setup();
      renderSheet({ onAddTag, allTags });

      const tagInput = screen.getByLabelText('New tag');
      await user.type(tagInput, 'Shopping');
      await user.click(screen.getByRole('button', { name: /add/i }));

      // 'shopping' (lowercase) is already on the task — should not call onAddTag
      expect(onAddTag).not.toHaveBeenCalled();
      expect(tagInput).toHaveValue('');
    });

    it('should not show suggestions when allTags is not provided', async () => {
      const user = userEvent.setup();
      renderSheet(); // no allTags prop

      const tagInput = screen.getByLabelText('New tag');
      await user.click(tagInput);

      expect(screen.queryByRole('option')).not.toBeInTheDocument();
    });

    it('should show no suggestions when all existing tags are already on the task', async () => {
      const user = userEvent.setup();
      // Task already has both tags from allTags
      renderSheet({ allTags: ['shopping', 'urgent'] });

      const tagInput = screen.getByLabelText('New tag');
      await user.click(tagInput);

      expect(screen.queryByRole('option')).not.toBeInTheDocument();
    });
  });

  describe('flush on close', () => {
    it('should flush pending description changes when sheet closes via Escape', async () => {
      const onUpdateDescription = vi.fn();
      const onClose = vi.fn();
      const user = userEvent.setup();
      renderSheet({ onUpdateDescription, onClose });

      // Modify description without blurring
      const textarea = screen.getByDisplayValue('Milk, eggs, bread');
      await user.clear(textarea);
      await user.type(textarea, 'Updated grocery list');

      // Escape closes the sheet — should flush pending description
      await user.keyboard('{Escape}');

      expect(onUpdateDescription).toHaveBeenCalledWith('Updated grocery list');
      expect(onClose).toHaveBeenCalled();
    });

    it('should not call onUpdateDescription on close when description has not changed', async () => {
      const onUpdateDescription = vi.fn();
      const onClose = vi.fn();
      const user = userEvent.setup();
      renderSheet({ onUpdateDescription, onClose });

      // Focus the textarea but don't change its value
      await user.click(screen.getByDisplayValue('Milk, eggs, bread'));
      await user.keyboard('{Escape}');

      expect(onUpdateDescription).not.toHaveBeenCalled();
      expect(onClose).toHaveBeenCalled();
    });
  });

  describe('save indicator', () => {
    it('should show saving indicator when saveStatus is pending', () => {
      renderSheet({ saveStatus: 'pending' });
      expect(screen.getByText('Saving...')).toBeInTheDocument();
    });

    it('should show saved indicator when saveStatus is success', () => {
      renderSheet({ saveStatus: 'success' });
      expect(screen.getByText('Saved')).toBeInTheDocument();
    });

    it('should not show save indicator when saveStatus is idle', () => {
      renderSheet({ saveStatus: 'idle' });
      expect(screen.queryByText('Saving...')).not.toBeInTheDocument();
      expect(screen.queryByText('Saved')).not.toBeInTheDocument();
    });
  });
});
