import { apiClient } from '@/lib/api-client';
import type { OnboardingStatus } from '../types/onboarding.types';

export const onboardingApi = {
  getStatus: () => apiClient.get<OnboardingStatus>('/api/onboarding/status'),
  complete: () => apiClient.post<OnboardingStatus>('/api/onboarding/complete'),
};
