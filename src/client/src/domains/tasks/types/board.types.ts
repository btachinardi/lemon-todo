import type { TaskStatus } from './task.types';

export interface Column {
  id: string;
  name: string;
  targetStatus: TaskStatus;
  position: number;
  maxTasks: number | null;
}

export interface Board {
  id: string;
  name: string;
  columns: Column[];
  createdAt: string;
}
