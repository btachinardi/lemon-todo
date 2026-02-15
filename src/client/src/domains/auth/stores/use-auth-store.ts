import { create } from 'zustand';
import type { UserProfile } from '../types/auth.types';

interface AuthState {
  accessToken: string | null;
  user: UserProfile | null;
  isAuthenticated: boolean;
  setAuth: (accessToken: string, user: UserProfile) => void;
  setAccessToken: (accessToken: string) => void;
  logout: () => void;
}

/**
 * Zustand store for authentication state. Memory-only — no persistence.
 *
 * Access token is kept in JS memory (never localStorage).
 * Refresh token is handled server-side via HttpOnly cookie.
 * On page refresh, AuthHydrationProvider performs a silent refresh
 * to restore the session from the cookie.
 */
export const useAuthStore = create<AuthState>()((set) => ({
  accessToken: null,
  user: null,
  isAuthenticated: false,
  setAuth: (accessToken, user) =>
    set({ accessToken, user, isAuthenticated: true }),
  setAccessToken: (accessToken) => set({ accessToken }),
  logout: () =>
    set({ accessToken: null, user: null, isAuthenticated: false }),
}));
