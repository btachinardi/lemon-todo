import { getAuthToken } from './auth.helpers';
import { API_BASE } from './e2e.config';

interface OnboardingStatusResponse {
  completed: boolean;
  completedAt: string | null;
}

/** Builds request headers with auth token. */
async function authHeaders(): Promise<Record<string, string>> {
  const token = await getAuthToken();
  return {
    'Content-Type': 'application/json',
    Authorization: `Bearer ${token}`,
  };
}

/** Gets the current user's onboarding status. */
export async function getOnboardingStatus(): Promise<OnboardingStatusResponse> {
  const res = await fetch(`${API_BASE}/onboarding/status`, {
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Get onboarding status failed: ${res.status}`);
  return res.json();
}

/** Marks onboarding as complete for the current user. */
export async function completeOnboarding(): Promise<OnboardingStatusResponse> {
  const res = await fetch(`${API_BASE}/onboarding/complete`, {
    method: 'POST',
    headers: await authHeaders(),
  });
  if (!res.ok) throw new Error(`Complete onboarding failed: ${res.status}`);
  return res.json();
}
