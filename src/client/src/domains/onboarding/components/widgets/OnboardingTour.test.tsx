import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, act } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createElement, type ReactNode } from 'react';
import { OnboardingTour } from './OnboardingTour';

// Mock the onboarding API
const mockGetStatus = vi.fn();
const mockComplete = vi.fn();
vi.mock('../../api/onboarding.api', () => ({
  onboardingApi: {
    get getStatus() { return mockGetStatus; },
    get complete() { return mockComplete; },
  },
}));

// Mock OnboardingTooltip to always render children when visible
vi.mock('../atoms/OnboardingTooltip', () => ({
  OnboardingTooltip: ({ visible, children }: { visible: boolean; children: ReactNode }) =>
    visible ? createElement('div', { 'data-testid': 'tooltip' }, children) : null,
}));

// Mock CelebrationAnimation
vi.mock('../atoms/CelebrationAnimation', () => ({
  CelebrationAnimation: ({ visible, onComplete }: { visible: boolean; onComplete: () => void }) =>
    visible ? createElement('div', { 'data-testid': 'celebration', onClick: onComplete }, 'All set!') : null,
}));

function createWrapper() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return createElement(QueryClientProvider, { client: queryClient }, children);
  };
}

describe('OnboardingTour', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockComplete.mockResolvedValue({ completed: true, completedAt: '2025-01-01T00:00:00Z' });
  });

  afterEach(() => {
    // Clean up any elements added to body during tests
    document.querySelectorAll('[data-onboarding]').forEach((el) => el.remove());
  });

  it('should render step 1 when onboarding not completed', async () => {
    mockGetStatus.mockResolvedValue({ completed: false, completedAt: null });

    render(createElement(OnboardingTour), { wrapper: createWrapper() });

    expect(await screen.findByText('Create your first task')).toBeInTheDocument();
  });

  it('should render nothing when onboarding is completed', async () => {
    mockGetStatus.mockResolvedValue({ completed: true, completedAt: '2025-01-01T00:00:00Z' });

    render(createElement(OnboardingTour), { wrapper: createWrapper() });

    await vi.waitFor(() => {
      // Only the backdrop div or nothing — no tooltip content
      expect(screen.queryByTestId('tooltip')).not.toBeInTheDocument();
    });
  });

  it('should advance to step 2 when a task card appears', async () => {
    mockGetStatus.mockResolvedValue({ completed: false, completedAt: null });

    render(createElement(OnboardingTour), { wrapper: createWrapper() });

    expect(await screen.findByText('Create your first task')).toBeInTheDocument();

    // Simulate a task card appearing in the DOM
    await act(async () => {
      const taskCard = document.createElement('div');
      taskCard.setAttribute('data-onboarding', 'task-card');
      document.body.appendChild(taskCard);
    });

    expect(await screen.findByText('Complete your task')).toBeInTheDocument();
  });

  it('should advance to step 3 when a done task appears', async () => {
    mockGetStatus.mockResolvedValue({ completed: false, completedAt: null });

    // Pre-add a task card so we start at step 2
    const taskCard = document.createElement('div');
    taskCard.setAttribute('data-onboarding', 'task-card');
    document.body.appendChild(taskCard);

    render(createElement(OnboardingTour), { wrapper: createWrapper() });

    expect(await screen.findByText('Complete your task')).toBeInTheDocument();

    // Simulate task completion
    await act(async () => {
      const doneTask = document.createElement('div');
      doneTask.setAttribute('data-onboarding', 'task-done');
      document.body.appendChild(doneTask);
    });

    expect(await screen.findByText('Explore your board!')).toBeInTheDocument();
  });

  it('should call complete API when skip is clicked', async () => {
    mockGetStatus.mockResolvedValue({ completed: false, completedAt: null });

    const user = userEvent.setup();
    render(createElement(OnboardingTour), { wrapper: createWrapper() });

    const skipButton = await screen.findByText('Skip tour');
    await user.click(skipButton);

    expect(mockComplete).toHaveBeenCalled();
  });

  it('should call complete API when finish is clicked at step 3', async () => {
    mockGetStatus.mockResolvedValue({ completed: false, completedAt: null });

    // Pre-add both markers to get to step 3
    const taskCard = document.createElement('div');
    taskCard.setAttribute('data-onboarding', 'task-card');
    document.body.appendChild(taskCard);

    const doneTask = document.createElement('div');
    doneTask.setAttribute('data-onboarding', 'task-done');
    document.body.appendChild(doneTask);

    const user = userEvent.setup();
    render(createElement(OnboardingTour), { wrapper: createWrapper() });

    const finishButton = await screen.findByText('Got it!');
    await user.click(finishButton);

    // Should show celebration
    expect(screen.getByText('All set!')).toBeInTheDocument();

    // Click celebration to trigger onComplete callback
    await user.click(screen.getByTestId('celebration'));

    expect(mockComplete).toHaveBeenCalled();
  });

  it('should show step indicators with correct label', async () => {
    mockGetStatus.mockResolvedValue({ completed: false, completedAt: null });

    render(createElement(OnboardingTour), { wrapper: createWrapper() });

    const indicator = await screen.findByLabelText('Step 1 of 3');
    expect(indicator).toBeInTheDocument();
  });
});
