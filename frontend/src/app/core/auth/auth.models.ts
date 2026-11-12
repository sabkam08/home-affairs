export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  userId: string;
  accessToken: string;

  expiresAtUtc?: string;
  expiresIn?: number;
  expiresInSeconds?: number;

  role?: string;
  roles?: string[];
}

export interface CurrentUser {
  userId?: string;
  email?: string;
  role?: string;
  roles?: string[];

  [key: string]: unknown;
}

export type PermissionResponse =
  | string[]
  | {
      permissions: string[];
    };

export interface AuthSession {
  userId: string;
  accessToken: string;
  expiresAtUtc: string | null;
  roles: string[];
}