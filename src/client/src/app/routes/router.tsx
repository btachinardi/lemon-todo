import { createBrowserRouter, Outlet } from 'react-router';
import { NotFoundPage } from '../pages/NotFoundPage';
import { LoginRoute } from './LoginRoute';
import { RegisterRoute } from './RegisterRoute';
import { ProtectedRoute } from './ProtectedRoute';
import { AdminRoute } from './AdminRoute';
import { TaskBoardRoute } from './TaskBoardRoute';
import { TaskListRoute } from './TaskListRoute';
import { AdminUsersPage } from '../pages/AdminUsersPage';
import { AdminAuditPage } from '../pages/AdminAuditPage';

/**
 * Application route tree. Auth routes are public; task routes are protected.
 * Admin routes require authentication (server enforces Admin+ role).
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
    element: (
      <AdminRoute>
        <Outlet />
      </AdminRoute>
    ),
    children: [
      {
        path: '/admin/users',
        element: <AdminUsersPage />,
      },
      {
        path: '/admin/audit',
        element: <AdminAuditPage />,
      },
    ],
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);
