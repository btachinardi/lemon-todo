import { DashboardLayout } from '../layouts/DashboardLayout';
import { TaskBoardPage } from '../pages/TaskBoardPage';

/** Route wrapper that renders the kanban board inside the dashboard shell. */
export function TaskBoardRoute() {
  return (
    <DashboardLayout>
      <TaskBoardPage />
    </DashboardLayout>
  );
}
