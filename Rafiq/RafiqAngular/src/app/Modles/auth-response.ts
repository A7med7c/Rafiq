export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  refreshTokenExpiresAt: string;
}

export interface AuthResponse {
  success: boolean;
  message: string;
  data: AuthTokens;
  errors: string[] | null;
}
