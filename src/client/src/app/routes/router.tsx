import { createBrowserRouter, Outlet } from 'react-router';
import { NotFoundPage } from '../pages/NotFoundPage';
import { LoginRoute } from './LoginRoute';
import { RegisterRoute } from './RegisterRoute';
import { ProtectedRoute } from './ProtectedRoute';
import { TaskBoardRoute } from './TaskBoardRoute';
import { TaskListRoute } from './TaskListRoute';

/**
 * Application route tree. Auth routes are public; task routes are protected.
 * Unauthenticated users are redirected to `/login`.
 */
export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginRoute />,
  },
  {
    path: '/register',
    element: <RegisterRoute />,
  },
  {
    element: (
      <ProtectedRoute>
        <Outlet />
      </ProtectedRoute>
    ),
    children: [
      {
        path: '/',
        element: <TaskBoardRoute />,
      },
      {
        path: '/list',
        element: <TaskListRoute />,
      },
    ],
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);
