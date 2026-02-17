import { useAuthStore } from '../stores/use-auth-store';
import { DEV_ACCOUNTS } from '../components/DevAccountSwitcher';
import { useDemoAccountsEnabled } from '@/domains/config/hooks/use-config';

/** Returns the demo account password for the currently logged-in user, or null if not a demo account or demo mode is disabled. */
export function useDevAccountPassword(): string | null {
  const email = useAuthStore((s) => s.user?.email);
  const { data: demoEnabled } = useDemoAccountsEnabled();

  if (!demoEnabled) return null;
  if (!email) return null;

  const match = DEV_ACCOUNTS.find((a) => a.email === email);
  return match?.password ?? null;
}
