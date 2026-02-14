import { DashboardLayout } from '../layouts/DashboardLayout';
import { TaskListPage } from '../pages/TaskListPage';

export function TaskListRoute() {
  return (
    <DashboardLayout>
      <TaskListPage />
    </DashboardLayout>
  );
}
