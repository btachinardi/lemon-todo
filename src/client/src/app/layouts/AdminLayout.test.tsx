import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { AdminLayout } from './AdminLayout';

// Mock sonner — capture props for Toaster configuration assertions
vi.mock('sonner', () => ({
  Toaster: (props: Record<string, unknown>) => (
    <div data-testid="sonner-toaster" {...Object.fromEntries(
      Object.entries(props).map(([k, v]) => [`data-${k.toLowerCase()}`, typeof v === 'object' ? JSON.stringify(v) : String(v)])
    )} />
  ),
}));

// Mock UserMenu to avoid auth mutation hooks — captures variant prop
vi.mock('@/domains/auth/components/UserMenu', () => ({
  UserMenu: (props: Record<string, unknown>) => (
    <div data-testid="user-menu" data-variant={props.variant ?? undefined} />
  ),
}));

// Mock LanguageSwitcher for isolation — captures props
vi.mock('@/domains/tasks/components/atoms/LanguageSwitcher', () => ({
  LanguageSwitcher: (props: Record<string, unknown>) => (
    <div data-testid="language-switcher" data-show-label={props.showLabel ? 'true' : undefined} />
  ),
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

  it('should configure toaster with a close button for manual dismissal', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    const toaster = screen.getByTestId('sonner-toaster');
    expect(toaster).toHaveAttribute('data-closebutton', 'true');
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

  it('should render a mobile menu toggle button in the header', () => {
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /menu/i });
    expect(menuButton).toBeInTheDocument();
  });

  it('should show tools in mobile menu when toggle is clicked', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /menu/i });
    await user.click(menuButton);

    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByTestId('language-switcher')).toBeInTheDocument();
    expect(within(dialog).getByTestId('user-menu')).toBeInTheDocument();
  });

  it('should pass showLabel to language switcher in mobile menu', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /menu/i });
    await user.click(menuButton);

    const dialog = screen.getByRole('dialog');
    const langSwitcher = within(dialog).getByTestId('language-switcher');
    expect(langSwitcher).toHaveAttribute('data-show-label', 'true');
  });

  it('should pass variant="inline" to UserMenu in mobile menu', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={['/admin/users']}>
        <AdminLayout>
          <p>Admin content</p>
        </AdminLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /menu/i });
    await user.click(menuButton);

    const dialog = screen.getByRole('dialog');
    const userMenu = within(dialog).getByTestId('user-menu');
    expect(userMenu).toHaveAttribute('data-variant', 'inline');
  });
});
