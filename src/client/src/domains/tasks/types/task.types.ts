export const Priority = {
  None: 'None',
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical',
} as const;

export type Priority = (typeof Priority)[keyof typeof Priority];

export const TaskStatus = {
  Todo: 'Todo',
  InProgress: 'InProgress',
  Done: 'Done',
  Archived: 'Archived',
} as const;

export type TaskStatus = (typeof TaskStatus)[keyof typeof TaskStatus];

export interface BoardTask {
  id: string;
  title: string;
  description: string | null;
  priority: Priority;
  status: TaskStatus;
  dueDate: string | null;
  tags: string[];
  columnId: string | null;
  position: number;
  isArchived: boolean;
  isDeleted: boolean;
  completedAt: string | null;
  createdAt: string;
  updatedAt: string;
}
