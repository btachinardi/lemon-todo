import { DashboardLayout } from '../layouts/DashboardLayout';
import { TaskBoardPage } from '../pages/TaskBoardPage';

export function TaskBoardRoute() {
  return (
    <DashboardLayout>
      <TaskBoardPage />
    </DashboardLayout>
  );
}
