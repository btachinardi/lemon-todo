import { DashboardLayout } from '../layouts/DashboardLayout';
import { TaskBoardPage } from '../pages/TaskBoardPage';
import { RouteErrorBoundary } from '@/ui/feedback/RouteErrorBoundary';

/** Route wrapper that renders the kanban board inside the dashboard shell. */
export function TaskBoardRoute() {
  return (
    <DashboardLayout>
      <RouteErrorBoundary>
        <TaskBoardPage />
      </RouteErrorBoundary>
    </DashboardLayout>
  );
}
