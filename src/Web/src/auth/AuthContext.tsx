import React, { useState, useEffect, useCallback } from 'react';
import { AuthUser, UserRole, LoginCredentials, ResetPasswordData } from '@/features/auth/types';
import { DEVELOPMENT_USERS } from '@/features/auth/mockData';
import { AuthContext } from './authContextDef';

const SESSION_STORAGE_KEY = 'bilim_akademi_dev_session';

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  // Restore session from sessionStorage on mount (strictly DEV-only)
  useEffect(() => {
    try {
      if (import.meta.env.DEV) {
        const stored = sessionStorage.getItem(SESSION_STORAGE_KEY);
        if (stored) {
          const parsed = JSON.parse(stored) as { role: UserRole };
          if (parsed.role && DEVELOPMENT_USERS[parsed.role]) {
            setUser(DEVELOPMENT_USERS[parsed.role]);
          }
        } else {
          // Default to student session in DEV mode for convenient preview
          setUser(DEVELOPMENT_USERS.student);
          sessionStorage.setItem(
            SESSION_STORAGE_KEY,
            JSON.stringify({ role: 'student' })
          );
        }
      } else {
        // In production: start with clean unauthenticated state
        setUser(null);
      }
    } catch {
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const loginAsRole = useCallback((selectedRole: UserRole) => {
    if (!import.meta.env.DEV) return;
    const selectedUser = DEVELOPMENT_USERS[selectedRole];
    if (selectedUser) {
      setUser(selectedUser);
      sessionStorage.setItem(
        SESSION_STORAGE_KEY,
        JSON.stringify({ role: selectedRole })
      );
    }
  }, []);

  const login = useCallback(
    async (credentials: LoginCredentials): Promise<void> => {
      setIsLoading(true);
      await new Promise((resolve) => setTimeout(resolve, 800));

      const input = credentials.emailOrUsername.toLowerCase().trim();
      let matchedRole: UserRole = 'student';

      if (input.includes('coach') || input.includes('koc') || input.includes('hasan')) {
        matchedRole = 'coach';
      } else if (input.includes('teacher') || input.includes('ogretmen') || input.includes('kemal')) {
        matchedRole = 'teacher';
      } else if (input.includes('parent') || input.includes('veli') || input.includes('merve')) {
        matchedRole = 'parent';
      } else if (input.includes('admin') || input.includes('mudur') || input.includes('ahmet')) {
        matchedRole = 'admin';
      }

      if (import.meta.env.DEV) {
        loginAsRole(matchedRole);
      } else {
        // In real production, this will be handled by the real AuthService / API
        setUser(DEVELOPMENT_USERS[matchedRole]);
      }
      setIsLoading(false);
    },
    [loginAsRole]
  );

  const logout = useCallback(() => {
    setUser(null);
    sessionStorage.removeItem(SESSION_STORAGE_KEY);
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
