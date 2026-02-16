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
  /**
   * Registers a new user account.
   * @param request - Email, password, and display name for the new account
   * @returns A new JWT access token and user profile on success
   * @throws {ApiRequestError} With 409 status if email is already registered, or 400 if validation fails
   */
  register(request: RegisterRequest): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>(`${BASE}/register`, request);
  },

  /**
   * Authenticates a user with email and password.
   * @param request - Email and password credentials
   * @returns A JWT access token and user profile on success
   * @throws {ApiRequestError} With 401 status if credentials are invalid, or 429 if account is locked
   */
  login(request: LoginRequest): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>(`${BASE}/login`, request);
  },

  /**
   * Refreshes the access token using the HttpOnly refresh token cookie.
   * No request body needed — server reads the cookie automatically.
   * @returns A new JWT access token and user profile on success
   * @throws {ApiRequestError} With 401 status if refresh token is invalid or expired
   */
  refresh(): Promise<AuthResponse> {
    return apiClient.post<AuthResponse>(`${BASE}/refresh`);
  },

  /**
   * Logs out the current user by invalidating the HttpOnly refresh token cookie.
   * No request body needed — server reads the cookie automatically.
   * @returns Void on success (clears the server-side cookie)
   * @throws {ApiRequestError} With 401 status if no valid refresh token cookie exists
   */
  logout(): Promise<void> {
    return apiClient.postVoid(`${BASE}/logout`);
  },

  /**
   * Fetches the current authenticated user's profile.
   * @returns The user's ID, email, display name, and roles
   * @throws {ApiRequestError} With 401 status if access token is missing or invalid
   */
  me(): Promise<UserProfile> {
    return apiClient.get<UserProfile>(`${BASE}/me`);
  },
};
