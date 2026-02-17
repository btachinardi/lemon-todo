import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { LandingLayout } from './LandingLayout';

// Mock AssignmentBanner to avoid external dependencies
vi.mock('@/domains/landing/components/widgets/AssignmentBanner', () => ({
  AssignmentBanner: () => null,
}));

describe('LandingLayout', () => {
  it('should render the Roadmap nav link in the header', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <LandingLayout>
          <p>Page content</p>
        </LandingLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const roadmapLink = within(header).getByRole('link', { name: 'Roadmap' });
    expect(roadmapLink).toHaveAttribute('href', '/roadmap');
  });

  it('should render all expected nav links in the header', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <LandingLayout>
          <p>Page content</p>
        </LandingLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    expect(within(header).getByRole('link', { name: 'Home' })).toHaveAttribute('href', '/');
    expect(within(header).getByRole('link', { name: 'Methodology' })).toHaveAttribute('href', '/methodology');
    expect(within(header).getByRole('link', { name: 'DevOps' })).toHaveAttribute('href', '/devops');
    expect(within(header).getByRole('link', { name: 'Roadmap' })).toHaveAttribute('href', '/roadmap');
  });

  it('should render a mobile menu toggle button in the header', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <LandingLayout>
          <p>Page content</p>
        </LandingLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /open menu/i });
    expect(menuButton).toBeInTheDocument();
  });

  it('should hide desktop nav links on mobile via responsive CSS classes', () => {
    render(
      <MemoryRouter initialEntries={['/']}>
        <LandingLayout>
          <p>Page content</p>
        </LandingLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const desktopNav = within(header).getByLabelText('Desktop navigation');
    expect(desktopNav.className).toMatch(/hidden/);
    expect(desktopNav.className).toMatch(/md:flex/);
  });

  it('should show nav links when mobile menu is opened', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={['/']}>
        <LandingLayout>
          <p>Page content</p>
        </LandingLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    const menuButton = within(header).getByRole('button', { name: /open menu/i });
    await user.click(menuButton);

    const mobileNav = within(header).getByLabelText('Mobile navigation');
    expect(within(mobileNav).getByRole('link', { name: 'Home' })).toBeInTheDocument();
    expect(within(mobileNav).getByRole('link', { name: 'Methodology' })).toBeInTheDocument();
    expect(within(mobileNav).getByRole('link', { name: 'DevOps' })).toBeInTheDocument();
    expect(within(mobileNav).getByRole('link', { name: 'Roadmap' })).toBeInTheDocument();
  });

  it('should close mobile menu when close button is clicked', async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={['/']}>
        <LandingLayout>
          <p>Page content</p>
        </LandingLayout>
      </MemoryRouter>,
    );

    const header = screen.getByRole('banner');
    await user.click(within(header).getByRole('button', { name: /open menu/i }));
    expect(within(header).getByLabelText('Mobile navigation')).toBeInTheDocument();

    await user.click(within(header).getByRole('button', { name: /close menu/i }));
    expect(within(header).queryByLabelText('Mobile navigation')).not.toBeInTheDocument();
  });
});
