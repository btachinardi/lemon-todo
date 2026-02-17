import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@testing-library/react';

// Mock react-i18next
vi.mock('react-i18next', () => ({
  useTranslation: () => ({
    t: (key: string, opts?: Record<string, unknown>) => {
      if (opts?.returnObjects) {
        return [
          { title: 'Dual DB Testing', description: 'Runs against both providers.' },
          { title: 'Docker Build', description: 'Multi-stage build.' },
        ];
      }
      return key;
    },
  }),
}));

// Mock motion/react – render standard HTML, bypass animations
vi.mock('motion/react', async () => {
  const React = await vi.importActual<typeof import('react')>('react');
  return {
    motion: {
      div: React.forwardRef(
        // eslint-disable-next-line @typescript-eslint/no-unused-vars
        ({ initial, animate, transition, variants, ...rest }: Record<string, unknown>, ref: unknown) =>
          React.createElement('div', { ...rest, ref }),
      ),
    },
    useInView: () => true,
  };
});

import { DevOpsPipelineSection } from './DevOpsPipelineSection';

describe('DevOpsPipelineSection', () => {
  describe('JobCard text wrapping prevention', () => {
    it('should apply whitespace-nowrap to time spans to prevent line breaking', () => {
      render(<DevOpsPipelineSection />);
      // Time spans appear in both desktop and mobile views
      const timeElements = screen.getAllByText('1m 23s');
      for (const el of timeElements) {
        expect(el.className).toContain('whitespace-nowrap');
      }
    });

    it('should apply truncate to name spans for CSS ellipsis on overflow', () => {
      render(<DevOpsPipelineSection />);
      // Use "Frontend Tests" which only appears in JobCard, not in detail cards
      const nameElements = screen.getAllByText('Frontend Tests');
      for (const el of nameElements) {
        expect(el.className).toContain('truncate');
      }
    });
  });

  describe('PipelineDiagram content', () => {
    it('should use full SQL Server name instead of hardcoded truncation', () => {
      render(<DevOpsPipelineSection />);
      // Hardcoded "SQL Serv..." should not exist — use CSS truncation instead
      expect(screen.queryByText(/SQL Serv\.\.\./)).not.toBeInTheDocument();
    });

    it('should apply overflow-hidden to absolutely-positioned card containers', () => {
      render(<DevOpsPipelineSection />);
      // Card containers in the diagram have both 'absolute' and 'rounded-xl' classes
      const allDivs = document.querySelectorAll('div');
      const cardContainers = Array.from(allDivs).filter(
        (el) => el.className.includes('absolute') && el.className.includes('rounded-xl'),
      );
      expect(cardContainers.length).toBeGreaterThan(0);
      for (const card of cardContainers) {
        expect(card.className).toContain('overflow-hidden');
      }
    });
  });
});
