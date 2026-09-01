import { createContext } from 'react';
import { AuthUser, UserRole, LoginCredentials, ResetPasswordData } from '@/features/auth/types';

export interface AuthContextType {
  user: AuthUser | null;
  role: UserRole | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginCredentials) => Promise<void>;
  loginAsRole: (role: UserRole) => void;
  logout: () => void;
  requestPasswordReset: (email: string) => Promise<void>;
  resetPassword: (data: ResetPasswordData) => Promise<void>;
}

export const AuthContext = createContext<AuthContextType | undefined>(undefined);
