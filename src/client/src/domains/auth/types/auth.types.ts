import type { components } from '../../../api/schema';

/** Payload for `POST /api/auth/register`. */
export type RegisterRequest = components['schemas']['RegisterRequest'];

/** Payload for `POST /api/auth/login`. */
export type LoginRequest = components['schemas']['LoginRequest'];

/** User profile returned in auth responses and from `GET /api/auth/me`. */
export type UserProfile = components['schemas']['UserResponse'];

/** Response from login, register, and refresh endpoints. Access token in body, refresh token in HttpOnly cookie. */
export type AuthResponse = components['schemas']['AuthResponse'];

/** Response from `POST /api/auth/reveal-profile` containing unredacted profile data. */
export type RevealedProfile = components['schemas']['RevealedProfileResponse'];
