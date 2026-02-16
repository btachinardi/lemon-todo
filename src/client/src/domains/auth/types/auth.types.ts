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

/** User profile returned in auth responses and from `GET /api/auth/me`. */
export interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  roles: string[];
}

/** Response from login, register, and refresh endpoints. Access token in body, refresh token in HttpOnly cookie. */
export interface AuthResponse {
  accessToken: string;
  user: UserProfile;
}
