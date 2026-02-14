import { createBrowserRouter } from 'react-router';
import { TaskBoardRoute } from './TaskBoardRoute';
import { TaskListRoute } from './TaskListRoute';
import { NotFoundPage } from '../pages/NotFoundPage';

export const router = createBrowserRouter([
  {
    path: '/',
    element: <TaskBoardRoute />,
  },
  {
    path: '/list',
    element: <TaskListRoute />,
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);
