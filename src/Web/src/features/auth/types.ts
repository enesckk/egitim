export type UserRole = 'student' | 'coach' | 'teacher' | 'parent' | 'admin';

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  initials: string;
  roleLabel: string;
  institutionName: string;
  branchName?: string;
  title?: string;
}

export interface LoginCredentials {
  emailOrUsername: string;
  password?: string;
}

export type AuthState =
  | 'unauthenticated'
  | 'authenticating'
  | 'authenticated'
  | 'session_checking';

export interface ResetPasswordData {
  token?: string;
  password?: string;
  confirmPassword?: string;
}
