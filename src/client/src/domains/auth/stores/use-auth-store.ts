import { create } from 'zustand';
import type { UserProfile } from '../types/auth.types';

interface AuthState {
  accessToken: string | null;
  user: UserProfile | null;
  isAuthenticated: boolean;
  /**
   * Sets both access token and user profile after a full authentication (login/register).
   * Marks the session as authenticated.
   * @param accessToken - JWT access token from the server
   * @param user - User profile containing ID, email, display name, and roles
   */
  setAuth: (accessToken: string, user: UserProfile) => void;
  /**
   * Updates only the access token after a silent refresh.
   * Does not change the user profile or authentication status — used when
   * AuthHydrationProvider refreshes the token on page load from the HttpOnly cookie.
   * @param accessToken - New JWT access token from the refresh endpoint
   */
  setAccessToken: (accessToken: string) => void;
  /**
   * Clears all authentication state (token, user, and isAuthenticated flag).
   * Called when the user explicitly logs out or when the refresh token expires.
   */
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
