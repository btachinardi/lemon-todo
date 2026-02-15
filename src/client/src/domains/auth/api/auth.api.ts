import { apiClient } from '@/lib/api-client';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  UserProfile,
} from '../types/auth.types';

const BASE = '/api/auth';

/** Client for the auth REST API (`/api/auth`). */
export const authApi = {
  register(request: RegisterRequest): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>(`${BASE}/register`, request);
  },

  login(request: LoginRequest): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>(`${BASE}/login`, request);
  },

  /** Refresh uses HttpOnly cookie — no request body needed. */
  refresh(): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>(`${BASE}/refresh`);
  },

  /** Logout uses HttpOnly cookie — no request body needed. */
  logout(): Promise<void> {
    return apiClient.postVoid(`${BASE}/logout`);
  },

  me(): Promise<UserProfile> {
    return apiClient.get<UserProfile>(`${BASE}/me`);
  },
};
