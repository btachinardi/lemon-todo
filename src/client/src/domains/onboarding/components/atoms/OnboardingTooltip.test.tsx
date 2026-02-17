import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, act } from '@testing-library/react';
import { OnboardingTooltip } from './OnboardingTooltip';

const TOOLTIP_WIDTH = 280;
const TOOLTIP_HEIGHT = 120;
const EDGE_PADDING = 12;

describe('OnboardingTooltip', () => {
  let target: HTMLDivElement;
  const originalInnerWidth = window.innerWidth;

  beforeEach(() => {
    target = document.createElement('div');
    target.id = 'test-target';
    document.body.appendChild(target);

    // jsdom doesn't compute layout — mock offset dimensions for tooltip measurement
    vi.spyOn(HTMLElement.prototype, 'offsetWidth', 'get').mockReturnValue(TOOLTIP_WIDTH);
    vi.spyOn(HTMLElement.prototype, 'offsetHeight', 'get').mockReturnValue(TOOLTIP_HEIGHT);
  });

  afterEach(() => {
    target.remove();
    vi.restoreAllMocks();
    Object.defineProperty(window, 'innerWidth', {
      value: originalInnerWidth,
      writable: true,
      configurable: true,
    });
  });

  function setViewportWidth(width: number) {
    Object.defineProperty(window, 'innerWidth', {
      value: width,
      writable: true,
      configurable: true,
    });
  }

  function mockTargetRect(rect: Partial<DOMRect>) {
    target.getBoundingClientRect = vi.fn(
      () =>
        ({
          top: 0,
          bottom: 0,
          left: 0,
          right: 0,
          width: 0,
          height: 0,
          x: 0,
          y: 0,
          toJSON: () => ({}),
          ...rect,
        }) as DOMRect,
    );
  }

  function getTooltip(container: HTMLElement) {
    return container.querySelector('[role="tooltip"]') as HTMLElement;
  }

  it('should render nothing when not visible', () => {
    mockTargetRect({ left: 100, width: 200, top: 50, bottom: 90 });

    const { container } = render(
      <OnboardingTooltip targetSelector="#test-target" position="bottom" visible={false}>
        <span>Content</span>
      </OnboardingTooltip>,
    );

    expect(getTooltip(container)).toBeNull();
  });

  it('should render tooltip near target when visible', async () => {
    setViewportWidth(1024);
    mockTargetRect({ left: 300, width: 200, top: 50, bottom: 90 });

    const { container } = render(
      <OnboardingTooltip targetSelector="#test-target" position="bottom" visible>
        <span>Content</span>
      </OnboardingTooltip>,
    );

    await act(async () => {});

    const tooltip = getTooltip(container);
    expect(tooltip).not.toBeNull();
    expect(tooltip.style.top).toBe(`${90 + EDGE_PADDING}px`);
  });

  describe('mobile viewport clamping', () => {
    it('should keep tooltip within right viewport boundary on mobile', async () => {
      setViewportWidth(375);
      // Target near the right edge of a mobile screen
      mockTargetRect({ left: 280, width: 80, top: 50, bottom: 90 });

      const { container } = render(
        <OnboardingTooltip targetSelector="#test-target" position="bottom" visible>
          <span>Content</span>
        </OnboardingTooltip>,
      );

      await act(async () => {});

      const tooltip = getTooltip(container);
      expect(tooltip).not.toBeNull();

      const left = parseFloat(tooltip.style.left);
      // Tooltip's right edge must not exceed viewport minus padding
      expect(left + TOOLTIP_WIDTH).toBeLessThanOrEqual(375 - EDGE_PADDING);
    });

    it('should keep tooltip within left viewport boundary on mobile', async () => {
      setViewportWidth(375);
      // Target partially scrolled off the left edge (e.g. horizontal-scroll board)
      mockTargetRect({ left: -20, width: 60, top: 50, bottom: 90 });

      const { container } = render(
        <OnboardingTooltip targetSelector="#test-target" position="bottom" visible>
          <span>Content</span>
        </OnboardingTooltip>,
      );

      await act(async () => {});

      const tooltip = getTooltip(container);
      expect(tooltip).not.toBeNull();

      const left = parseFloat(tooltip.style.left);
      // Tooltip's left edge must not go below minimum padding
      expect(left).toBeGreaterThanOrEqual(EDGE_PADDING);
    });

    it('should position arrow pointing at target center when tooltip is clamped right', async () => {
      setViewportWidth(375);
      mockTargetRect({ left: 280, width: 80, top: 50, bottom: 90 });

      const { container } = render(
        <OnboardingTooltip targetSelector="#test-target" position="bottom" visible>
          <span>Content</span>
        </OnboardingTooltip>,
      );

      await act(async () => {});

      const tooltip = getTooltip(container);
      expect(tooltip).not.toBeNull();

      const tooltipLeft = parseFloat(tooltip.style.left);
      const targetCenter = 280 + 40; // 320
      const expectedArrowLeft = targetCenter - tooltipLeft;

      // The arrow element is the first child div inside the tooltip
      const arrow = tooltip.firstElementChild as HTMLElement;
      expect(parseFloat(arrow.style.left)).toBe(expectedArrowLeft);
    });

    it('should clamp arrow to tooltip bounds when target center is outside tooltip', async () => {
      setViewportWidth(375);
      // Target partially off-screen left — target center (10px) is left of tooltip left edge (12px)
      mockTargetRect({ left: -20, width: 60, top: 50, bottom: 90 });

      const { container } = render(
        <OnboardingTooltip targetSelector="#test-target" position="bottom" visible>
          <span>Content</span>
        </OnboardingTooltip>,
      );

      await act(async () => {});

      const tooltip = getTooltip(container);
      expect(tooltip).not.toBeNull();

      // Arrow should be clamped to minimum 16px from tooltip edge (not negative)
      const arrow = tooltip.firstElementChild as HTMLElement;
      const arrowLeft = parseFloat(arrow.style.left);
      expect(arrowLeft).toBeGreaterThanOrEqual(16);
      expect(arrowLeft).toBeLessThanOrEqual(TOOLTIP_WIDTH - 16);
    });
  });
});
