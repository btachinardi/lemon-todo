import { useMutation, useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/auth.api';
import { useAuthStore } from '../stores/use-auth-store';
import type { LoginRequest, RegisterRequest } from '../types/auth.types';

/**
 * Registers a new user and stores the returned access token.
 * @returns A TanStack Query mutation result with `mutate()` to trigger registration,
 *          `isPending` for loading state, and `error` for failure handling.
 *          On success, automatically stores the access token and user in the auth store.
 */
export function useRegister() {
  const setAuth = useAuthStore((s) => s.setAuth);
  return useMutation({
    mutationFn: (request: RegisterRequest) => authApi.register(request),
    onSuccess: (data) => {
      setAuth(data.accessToken, data.user);
    },
  });
}

/**
 * Logs in a user and stores the returned access token.
 * @returns A TanStack Query mutation result with `mutate()` to trigger login,
 *          `isPending` for loading state, and `error` for failure handling.
 *          On success, automatically stores the access token and user in the auth store.
 */
export function useLogin() {
  const setAuth = useAuthStore((s) => s.setAuth);
  return useMutation({
    mutationFn: (request: LoginRequest) => authApi.login(request),
    onSuccess: (data) => {
      setAuth(data.accessToken, data.user);
    },
  });
}

/**
 * Logs out the current user via HttpOnly cookie and clears all caches.
 * @returns A TanStack Query mutation result with `mutate()` to trigger logout.
 *          On completion (success or failure), clears the auth store and all TanStack Query caches.
 */
export function useLogout() {
  const queryClient = useQueryClient();
  const logout = useAuthStore((s) => s.logout);

  return useMutation({
    mutationFn: () => authApi.logout(),
    onSettled: () => {
      logout();
      queryClient.clear();
    },
  });
}
