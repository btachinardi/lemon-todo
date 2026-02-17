import { useAuthStore } from '../stores/use-auth-store';
import { DEV_ACCOUNTS } from '../components/DevAccountSwitcher';

/** Returns the demo account password for the currently logged-in user, or null if not a demo account or not in dev mode. */
export function useDevAccountPassword(): string | null {
  const email = useAuthStore((s) => s.user?.email);

  if (!import.meta.env.DEV) return null;
  if (!email) return null;

  const match = DEV_ACCOUNTS.find((a) => a.email === email);
  return match?.password ?? null;
}
