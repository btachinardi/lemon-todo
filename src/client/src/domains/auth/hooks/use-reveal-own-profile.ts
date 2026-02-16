import { useMutation } from '@tanstack/react-query';
import { authApi } from '../api/auth.api';

/** Reveals the current user's own protected profile data after password re-authentication. */
export function useRevealOwnProfile() {
  return useMutation({
    mutationFn: (password: string) => authApi.revealProfile(password),
  });
}
