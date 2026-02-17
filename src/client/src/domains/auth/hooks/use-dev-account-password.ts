import { useAuthStore } from '../stores/use-auth-store';
import { DEV_ACCOUNTS } from '../components/DevAccountSwitcher';
import { useDemoAccountsEnabled } from '@/domains/config/hooks/use-config';

/** The email domain used exclusively by seeded demo accounts. */
const DEMO_EMAIL_DOMAIN = '@lemondo.dev';

/**
 * Returns the demo account password for the currently logged-in user, or null
 * if not a demo account or demo mode is disabled.
 *
 * The API returns redacted emails (e.g. "d***@lemondo.dev") so we can't match
 * by exact email. Instead we check the preserved domain suffix and use roles
 * to determine which demo account is active.
 */
export function useDevAccountPassword(): string | null {
  const email = useAuthStore((s) => s.user?.email);
  const roles = useAuthStore((s) => s.user?.roles);
  const { data: demoEnabled } = useDemoAccountsEnabled();

  if (!demoEnabled) return null;
  if (!email || !roles) return null;
  if (!email.endsWith(DEMO_EMAIL_DOMAIN)) return null;

  // Match by highest role — demo accounts have distinct role sets
  if (roles.includes('SystemAdmin'))
    return DEV_ACCOUNTS.find((a) => a.roleKey === 'sysadmin')!.password;
  if (roles.includes('Admin'))
    return DEV_ACCOUNTS.find((a) => a.roleKey === 'admin')!.password;
  return DEV_ACCOUNTS.find((a) => a.roleKey === 'user')!.password;
}
