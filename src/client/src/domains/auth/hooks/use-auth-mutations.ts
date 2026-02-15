import { useMutation, useQueryClient } from '@tanstack/react-query';
import { authApi } from '../api/auth.api';
import { useAuthStore } from '../stores/use-auth-store';
import type { LoginRequest, RegisterRequest } from '../types/auth.types';

/** Registers a new user and stores the returned tokens. */
export function useRegister() {
  const setAuth = useAuthStore((s) => s.setAuth);
  return useMutation({
    mutationFn: (request: RegisterRequest) => authApi.register(request),
    onSuccess: (data) => {
      setAuth(data.accessToken, data.refreshToken, data.user);
    },
  });
}

/** Logs in a user and stores the returned tokens. */
export function useLogin() {
  const setAuth = useAuthStore((s) => s.setAuth);
  return useMutation({
    mutationFn: (request: LoginRequest) => authApi.login(request),
    onSuccess: (data) => {
      setAuth(data.accessToken, data.refreshToken, data.user);
    },
  });
}

/** Logs out the current user, revokes the refresh token, and clears all caches. */
export function useLogout() {
  const queryClient = useQueryClient();
  const refreshToken = useAuthStore((s) => s.refreshToken);
  const logout = useAuthStore((s) => s.logout);

  return useMutation({
    mutationFn: () => {
      if (refreshToken) {
        return authApi.logout({ refreshToken });
      }
      return Promise.resolve();
    },
    onSettled: () => {
      logout();
      queryClient.clear();
    },
  });
}
