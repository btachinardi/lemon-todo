import { describe, it, expect, vi } from 'vitest';
import { render, screen, within } from '@testing-library/react';
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
});
