import { describe, it, expect } from 'vitest';
import { groupTasksByDate } from './group-tasks';
import { createTask } from '@/test/factories';
import { TaskStatus } from '../types/task.types';
import { GroupBy } from '../types/grouping.types';

describe('groupTasksByDate', () => {
  describe('when groupBy is none', () => {
    it('should return a single flat group with all tasks', () => {
      const tasks = [
        createTask({ title: 'A', createdAt: '2026-01-15T10:00:00Z' }),
        createTask({ title: 'B', createdAt: '2026-01-14T10:00:00Z' }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.None, false);

      expect(groups).toHaveLength(1);
      expect(groups[0].key).toBe('all');
      expect(groups[0].label).toBe('All Tasks');
      expect(groups[0].tasks).toHaveLength(2);
      expect(groups[0].completedTasks).toHaveLength(0);
    });

    it('should include done tasks in the tasks array when not splitting', () => {
      const tasks = [
        createTask({ title: 'Active' }),
        createTask({ title: 'Done', status: TaskStatus.Done }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.None, false);

      expect(groups[0].tasks).toHaveLength(2);
      expect(groups[0].completedTasks).toHaveLength(0);
    });

    it('should split done tasks when splitCompleted is true', () => {
      const tasks = [
        createTask({ title: 'Active' }),
        createTask({ title: 'Done', status: TaskStatus.Done }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.None, true);

      expect(groups[0].tasks).toHaveLength(1);
      expect(groups[0].tasks[0].title).toBe('Active');
      expect(groups[0].completedTasks).toHaveLength(1);
      expect(groups[0].completedTasks[0].title).toBe('Done');
    });
  });

  describe('when groupBy is day', () => {
    it('should group tasks by calendar day', () => {
      const tasks = [
        createTask({ title: 'Jan 15 A', createdAt: '2026-01-15T10:00:00Z' }),
        createTask({ title: 'Jan 15 B', createdAt: '2026-01-15T14:00:00Z' }),
        createTask({ title: 'Jan 14', createdAt: '2026-01-14T10:00:00Z' }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.Day, false);

      expect(groups).toHaveLength(2);
      expect(groups[0].key).toBe('2026-01-15');
      expect(groups[0].tasks).toHaveLength(2);
      expect(groups[1].key).toBe('2026-01-14');
      expect(groups[1].tasks).toHaveLength(1);
    });

    it('should produce labels like "Wed, Jan 15, 2026"', () => {
      const tasks = [createTask({ createdAt: '2026-01-15T10:00:00Z' })];
      const groups = groupTasksByDate(tasks, GroupBy.Day, false);
      expect(groups[0].label).toBe('Thu, Jan 15, 2026');
    });

    it('should order groups reverse-chronologically', () => {
      const tasks = [
        createTask({ createdAt: '2026-01-10T00:00:00Z' }),
        createTask({ createdAt: '2026-01-20T00:00:00Z' }),
        createTask({ createdAt: '2026-01-15T00:00:00Z' }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.Day, false);
      const keys = groups.map((g) => g.key);

      expect(keys).toEqual(['2026-01-20', '2026-01-15', '2026-01-10']);
    });
  });

  describe('when groupBy is week', () => {
    it('should group tasks by ISO week', () => {
      const tasks = [
        createTask({ createdAt: '2026-01-15T10:00:00Z' }), // Wed, week of Jan 12
        createTask({ createdAt: '2026-01-13T10:00:00Z' }), // Mon, same week
        createTask({ createdAt: '2026-01-05T10:00:00Z' }), // Mon, prior week
      ];

      const groups = groupTasksByDate(tasks, GroupBy.Week, false);

      expect(groups).toHaveLength(2);
      expect(groups[0].tasks).toHaveLength(2);
      expect(groups[1].tasks).toHaveLength(1);
    });

    it('should produce labels like "Week of Jan 12, 2026"', () => {
      const tasks = [createTask({ createdAt: '2026-01-15T10:00:00Z' })];
      const groups = groupTasksByDate(tasks, GroupBy.Week, false);
      expect(groups[0].label).toBe('Week of Jan 12, 2026');
    });
  });

  describe('when groupBy is month', () => {
    it('should group tasks by calendar month', () => {
      const tasks = [
        createTask({ createdAt: '2026-01-15T10:00:00Z' }),
        createTask({ createdAt: '2026-01-25T10:00:00Z' }),
        createTask({ createdAt: '2026-02-01T10:00:00Z' }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.Month, false);

      expect(groups).toHaveLength(2);
      expect(groups[0].key).toBe('2026-02');
      expect(groups[0].tasks).toHaveLength(1);
      expect(groups[1].key).toBe('2026-01');
      expect(groups[1].tasks).toHaveLength(2);
    });

    it('should produce labels like "January 2026"', () => {
      const tasks = [createTask({ createdAt: '2026-01-15T10:00:00Z' })];
      const groups = groupTasksByDate(tasks, GroupBy.Month, false);
      expect(groups[0].label).toBe('January 2026');
    });
  });

  describe('splitCompleted with grouping', () => {
    it('should partition done tasks into completedTasks within each group', () => {
      const tasks = [
        createTask({ title: 'Active Jan 15', createdAt: '2026-01-15T10:00:00Z' }),
        createTask({ title: 'Done Jan 15', createdAt: '2026-01-15T14:00:00Z', status: TaskStatus.Done }),
        createTask({ title: 'Active Jan 14', createdAt: '2026-01-14T10:00:00Z' }),
        createTask({ title: 'Done Jan 14', createdAt: '2026-01-14T14:00:00Z', status: TaskStatus.Done }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.Day, true);

      expect(groups).toHaveLength(2);
      expect(groups[0].tasks).toHaveLength(1);
      expect(groups[0].tasks[0].title).toBe('Active Jan 15');
      expect(groups[0].completedTasks).toHaveLength(1);
      expect(groups[0].completedTasks[0].title).toBe('Done Jan 15');
      expect(groups[1].tasks).toHaveLength(1);
      expect(groups[1].completedTasks).toHaveLength(1);
    });

    it('should leave completedTasks empty when not splitting', () => {
      const tasks = [
        createTask({ status: TaskStatus.Done, createdAt: '2026-01-15T10:00:00Z' }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.Day, false);

      expect(groups[0].tasks).toHaveLength(1);
      expect(groups[0].completedTasks).toHaveLength(0);
    });
  });

  describe('edge cases', () => {
    it('should return empty array for empty input', () => {
      expect(groupTasksByDate([], GroupBy.Day, false)).toEqual([]);
    });

    it('should return empty array for empty input with none grouping', () => {
      expect(groupTasksByDate([], GroupBy.None, false)).toEqual([]);
    });

    it('should handle a group with only completed tasks', () => {
      const tasks = [
        createTask({ status: TaskStatus.Done, createdAt: '2026-01-15T10:00:00Z' }),
      ];

      const groups = groupTasksByDate(tasks, GroupBy.Day, true);

      expect(groups[0].tasks).toHaveLength(0);
      expect(groups[0].completedTasks).toHaveLength(1);
    });
  });
});
