import type { TaskStatus } from './task.types';

export interface Column {
  id: string;
  name: string;
  targetStatus: TaskStatus;
  position: number;
  maxTasks: number | null;
}

export interface TaskCard {
  taskId: string;
  columnId: string;
  position: number;
}

export interface Board {
  id: string;
  name: string;
  columns: Column[];
  cards?: TaskCard[];
  createdAt: string;
}
