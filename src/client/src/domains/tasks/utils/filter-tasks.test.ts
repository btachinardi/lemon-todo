import { describe, it, expect } from 'vitest';
import * as fc from 'fast-check';
import { filterTasks, hasActiveFilters } from './filter-tasks';
import { createTask } from '@/test/factories';
import { Priority, TaskStatus } from '../types/task.types';

describe('filterTasks', () => {
  it('should return all tasks when no criteria set', () => {
    const tasks = [createTask({ title: 'A' }), createTask({ title: 'B' })];
    expect(filterTasks(tasks, {})).toHaveLength(2);
  });

  it('should filter by search term in title', () => {
    const tasks = [
      createTask({ title: 'Buy groceries' }),
      createTask({ title: 'Clean house' }),
    ];
    const result = filterTasks(tasks, { searchTerm: 'groc' });
    expect(result).toHaveLength(1);
    expect(result[0].title).toBe('Buy groceries');
  });

  it('should filter by search term in description', () => {
    const tasks = [
      createTask({ title: 'Task A', description: 'Get milk and eggs' }),
      createTask({ title: 'Task B', description: null }),
    ];
    const result = filterTasks(tasks, { searchTerm: 'milk' });
    expect(result).toHaveLength(1);
    expect(result[0].title).toBe('Task A');
  });

  it('should filter by priority', () => {
    const tasks = [
      createTask({ priority: Priority.High }),
      createTask({ priority: Priority.Low }),
    ];
    const result = filterTasks(tasks, { filterPriority: Priority.High });
    expect(result).toHaveLength(1);
    expect(result[0].priority).toBe(Priority.High);
  });

  it('should filter by status', () => {
    const tasks = [
      createTask({ status: TaskStatus.Done }),
      createTask({ status: TaskStatus.Todo }),
    ];
    const result = filterTasks(tasks, { filterStatus: TaskStatus.Done });
    expect(result).toHaveLength(1);
    expect(result[0].status).toBe(TaskStatus.Done);
  });

  it('should filter by tag', () => {
    const tasks = [
      createTask({ tags: ['work', 'urgent'] }),
      createTask({ tags: ['personal'] }),
    ];
    const result = filterTasks(tasks, { filterTag: 'urgent' });
    expect(result).toHaveLength(1);
    expect(result[0].tags).toContain('urgent');
  });

  it('should combine multiple filters with AND', () => {
    const tasks = [
      createTask({ title: 'Buy groceries', priority: Priority.High, tags: ['shopping'] }),
      createTask({ title: 'Buy milk', priority: Priority.Low, tags: ['shopping'] }),
      createTask({ title: 'Read book', priority: Priority.High, tags: ['leisure'] }),
    ];
    const result = filterTasks(tasks, {
      searchTerm: 'buy',
      filterPriority: Priority.High,
    });
    expect(result).toHaveLength(1);
    expect(result[0].title).toBe('Buy groceries');
  });

  it('property: filtering never adds tasks', () => {
    fc.assert(
      fc.property(
        fc.array(fc.record({
          title: fc.string({ minLength: 1, maxLength: 50 }),
          priority: fc.constantFrom(Priority.None, Priority.Low, Priority.Medium, Priority.High, Priority.Critical),
          status: fc.constantFrom(TaskStatus.Todo, TaskStatus.InProgress, TaskStatus.Done),
        }), { minLength: 0, maxLength: 20 }),
        fc.string({ minLength: 0, maxLength: 10 }),
        (taskPartials, term) => {
          const tasks = taskPartials.map((p) => createTask(p));
          const result = filterTasks(tasks, { searchTerm: term });
          return result.length <= tasks.length;
        },
      ),
    );
  });
});

describe('hasActiveFilters', () => {
  it('should return false for empty criteria', () => {
    expect(hasActiveFilters({})).toBe(false);
  });

  it('should return true when searchTerm is set', () => {
    expect(hasActiveFilters({ searchTerm: 'test' })).toBe(true);
  });

  it('should return true when filterPriority is set', () => {
    expect(hasActiveFilters({ filterPriority: 'High' })).toBe(true);
  });

  it('should return false for empty string searchTerm', () => {
    expect(hasActiveFilters({ searchTerm: '' })).toBe(false);
  });
});
