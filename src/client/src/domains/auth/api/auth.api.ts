import { apiClient } from '@/lib/api-client';
import type {
  AuthResponse,
  LoginRequest,
  RegisterRequest,
  RefreshRequest,
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

  refresh(request: RefreshRequest): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>(`${BASE}/refresh`, request);
  },

  logout(request: RefreshRequest): Promise<void> {
    return apiClient.postVoid(`${BASE}/logout`, request);
  },

  me(): Promise<UserProfile> {
    return apiClient.get<UserProfile>(`${BASE}/me`);
  },
};
