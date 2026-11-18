/** Types of the /api/auth/* endpoints (see specs/002-user-auth/contracts/auth.openapi.yaml). */

export interface UserDto {
  id: number;
  lastName: string;
  firstName: string;
  email: string;
  claims: string[];
}

export interface AuthSession {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  refreshTokenExpiresAt: string;
  user: UserDto;
}

export interface RegisterRequest {
  lastName: string;
  firstName: string;
  email: string;
  password: string;
  passwordConfirmation: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface StoredTokens {
  accessToken: string;
  refreshToken: string;
}

/** Error body of the API: { id, code, message, status, fields? }. */
export interface ApiError {
  id?: string;
  code: string;
  message: string;
  status: number;
  fields?: { field: string; message: string }[];
}

export function isApiError(body: unknown): body is ApiError {
  if (typeof body !== 'object' || body === null) {
    return false;
  }
  const candidate = body as Record<string, unknown>;
  return typeof candidate['code'] === 'string' && typeof candidate['message'] === 'string';
}
