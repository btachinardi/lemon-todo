import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router';
import { AdminLayout } from './AdminLayout';

// Mock sonner to avoid layout complexity
vi.mock('sonner', () => ({
  Toaster: () => null,
}));

// Mock UserMenu to avoid auth mutation hooks
vi.mock('@/domains/auth/components/UserMenu', () => ({
  UserMenu: () => <div data-testid="user-menu" />,
}));

// Mock LanguageSwitcher for isolation
vi.mock('@/domains/tasks/components/atoms/LanguageSwitcher', () => ({
  LanguageSwitcher: () => <div data-testid="language-switcher" />,
}));

describe('AdminLayout', () => {
  it('should render the language switcher in the header', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    expect(screen.getByTestId('language-switcher')).toBeInTheDocument();
  });

  it('should render the theme toggle', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    expect(screen.getByRole('button', { name: /theme/i })).toBeInTheDocument();
  });

  it('should render the user menu', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    expect(screen.getByTestId('user-menu')).toBeInTheDocument();
  });

  it('should render children content', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    expect(screen.getByText('Admin content')).toBeInTheDocument();
  });
});
