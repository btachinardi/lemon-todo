import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TaskStatusChip } from './TaskStatusChip';
import { TaskStatus } from '../../types/task.types';

describe('TaskStatusChip', () => {
  it('renders Todo status', () => {
    render(<TaskStatusChip status={TaskStatus.Todo} />);
    expect(screen.getByText('To Do')).toBeInTheDocument();
  });

  it('renders InProgress status', () => {
    render(<TaskStatusChip status={TaskStatus.InProgress} />);
    expect(screen.getByText('In Progress')).toBeInTheDocument();
  });

  it('renders Done status', () => {
    render(<TaskStatusChip status={TaskStatus.Done} />);
    expect(screen.getByText('Done')).toBeInTheDocument();
  });

  it('renders Archived status', () => {
    render(<TaskStatusChip status={TaskStatus.Archived} />);
    expect(screen.getByText('Archived')).toBeInTheDocument();
  });
});
