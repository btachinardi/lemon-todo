/** Payload for `POST /api/auth/register`. */
export interface RegisterRequest {
  email: string;
  password: string;
  displayName: string;
}

/** Payload for `POST /api/auth/login`. */
export interface LoginRequest {
  email: string;
  password: string;
}

/** Payload for `POST /api/auth/refresh`. */
export interface RefreshRequest {
  refreshToken: string;
}

/** User profile returned in auth responses and from `GET /api/auth/me`. */
export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
}

/** Response from login, register, and refresh endpoints. */
export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  user: UserProfile;
}
