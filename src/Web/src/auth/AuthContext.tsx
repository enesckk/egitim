import React, { useState, useEffect, useCallback } from 'react';
import { AuthUser, LoginCredentials, ResetPasswordData, UserRole } from '@/features/auth/types';
import { authService } from '@/services/auth';
import { AuthContext } from './authContextDef';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Initialize session on mount via silent refresh (HttpOnly cookie)
  useEffect(() => {
    let isMounted = true;

    // Listen to reactive auth state changes (e.g. 401 session expiry)
    const unsubscribe = authService.onAuthStateChanged((newUser) => {
      if (isMounted) {
        setUser(newUser);
      }
    });

    const initAuth = async () => {
      try {
        const initialUser = await authService.initialize();
        if (isMounted) {
          setUser(initialUser);
        }
      } catch {
        if (isMounted) {
          setUser(null);
        }
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    initAuth();

    return () => {
      isMounted = false;
      unsubscribe();
    };
  }, []);

  const login = useCallback(
    async (credentials: LoginCredentials): Promise<void> => {
      setIsLoading(true);
      try {
        const session = await authService.login({
          email: credentials.emailOrUsername,
          password: credentials.password,
        });
        setUser(session.user);
      } finally {
        setIsLoading(false);
      }
    },
    []
  );

  const loginAsRole = useCallback((selectedRole: UserRole) => {
    // Kept for interface compatibility; in production auth is backend-governed
    void selectedRole;
  }, []);

  const logout = useCallback(async () => {
    setIsLoading(true);
    try {
      await authService.logout();
    } finally {
      setUser(null);
      setIsLoading(false);
    }
  }, []);

  const requestPasswordReset = useCallback(async (email: string): Promise<void> => {
    void email;
    setIsLoading(true);
    await new Promise((resolve) => setTimeout(resolve, 800));
    setIsLoading(false);
  }, []);

  const resetPassword = useCallback(async (data: ResetPasswordData): Promise<void> => {
    void data;
    setIsLoading(true);
    await new Promise((resolve) => setTimeout(resolve, 800));
    setIsLoading(false);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        user,
        role: user ? user.role : null,
        isAuthenticated: !!user,
        isLoading,
        login,
        loginAsRole,
        logout,
        requestPasswordReset,
        resetPassword,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

