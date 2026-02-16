import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { AdminRoute } from './AdminRoute';
import { useAuthStore } from '@/domains/auth/stores/use-auth-store';

describe('AdminRoute', () => {
  beforeEach(() => {
    useAuthStore.setState({
      accessToken: null,
      user: null,
      isAuthenticated: false,
    });
  });

  it('should redirect to login when not authenticated', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminRoute>
          <p>Admin content</p>
        </AdminRoute>
      </MemoryRouter>,
    );
    expect(screen.queryByText('Admin content')).not.toBeInTheDocument();
  });

  it('should redirect away when authenticated but not admin', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'user@test.com', displayName: 'User', roles: ['User'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminRoute>
          <p>Admin content</p>
        </AdminRoute>
      </MemoryRouter>,
    );
    expect(screen.queryByText('Admin content')).not.toBeInTheDocument();
  });

  it('should render children when user has Admin role', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'admin@test.com', displayName: 'Admin', roles: ['User', 'Admin'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminRoute>
          <p>Admin content</p>
        </AdminRoute>
      </MemoryRouter>,
    );
    expect(screen.getByText('Admin content')).toBeInTheDocument();
  });

  it('should render children when user has SystemAdmin role', () => {
    useAuthStore.setState({
      accessToken: 'token',
      user: { id: '1', email: 'sysadmin@test.com', displayName: 'SysAdmin', roles: ['User', 'SystemAdmin'] },
      isAuthenticated: true,
    });

    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminRoute>
          <p>Admin content</p>
        </AdminRoute>
      </MemoryRouter>,
    );
    expect(screen.getByText('Admin content')).toBeInTheDocument();
  });
});
