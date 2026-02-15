import { DashboardLayout } from '../layouts/DashboardLayout';
import { TaskListPage } from '../pages/TaskListPage';
import { RouteErrorBoundary } from '@/ui/feedback/RouteErrorBoundary';

/** Route wrapper that renders the flat task list inside the dashboard shell. */
export function TaskListRoute() {
  return (
    <DashboardLayout>
      <RouteErrorBoundary>
        <TaskListPage />
      </RouteErrorBoundary>
    </DashboardLayout>
  );
}
