import { DashboardLayout } from '../layouts/DashboardLayout';
import { TaskListPage } from '../pages/TaskListPage';

/** Route wrapper that renders the flat task list inside the dashboard shell. */
export function TaskListRoute() {
  return (
    <DashboardLayout>
      <TaskListPage />
    </DashboardLayout>
  );
}
