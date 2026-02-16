import { createBrowserRouter, Outlet } from 'react-router';
import { NotFoundPage } from '../pages/NotFoundPage';
import { LandingPage } from '../pages/LandingPage';
import { StoryPage } from '../pages/StoryPage';
import { LoginRoute } from './LoginRoute';
import { RegisterRoute } from './RegisterRoute';
import { PublicRoute } from './PublicRoute';
import { ProtectedRoute } from './ProtectedRoute';
import { AdminRoute } from './AdminRoute';
import { TaskBoardRoute } from './TaskBoardRoute';
import { TaskListRoute } from './TaskListRoute';
import { AdminUsersPage } from '../pages/AdminUsersPage';
import { AdminAuditPage } from '../pages/AdminAuditPage';
import { LandingLayout } from '../layouts/LandingLayout';

/**
 * Application route tree.
 * - `/` is the public landing page (redirects to `/board` if authenticated)
 * - `/board` and `/list` are protected app routes
 * - `/admin/*` requires Admin+ role
 */
export const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <PublicRoute>
        <LandingLayout>
          <LandingPage />
        </LandingLayout>
      </PublicRoute>
    ),
  },
  {
    path: '/story',
    element: (
      <LandingLayout>
        <StoryPage />
      </LandingLayout>
    ),
  },
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
        path: '/board',
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
