import { createBrowserRouter, Navigate, Outlet } from 'react-router';
import { NotFoundPage } from '../pages/NotFoundPage';
import { LandingPage } from '../pages/LandingPage';
import { StoryPage } from '../pages/StoryPage';
import { RoadmapPage } from '../pages/RoadmapPage';
import { DevOpsPage } from '../pages/DevOpsPage';
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
import { LoadingPreviewPage } from '../pages/LoadingPreviewPage';

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
    path: '/methodology',
    element: (
      <LandingLayout>
        <StoryPage />
      </LandingLayout>
    ),
  },
  {
    path: '/story',
    element: <Navigate to="/methodology" replace />,
  },
  {
    path: '/roadmap',
    element: (
      <LandingLayout>
        <RoadmapPage />
      </LandingLayout>
    ),
  },
  {
    path: '/devops',
    element: (
      <LandingLayout>
        <DevOpsPage />
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
    path: '/loading',
    element: <LoadingPreviewPage />,
  },
  {
    path: '*',
    element: <NotFoundPage />,
  },
]);
