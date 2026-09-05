import { apiClient } from '../api/apiClient';
import { parseUserFromToken } from './jwtUtils';
import { AuthUser } from '@/features/auth/types';

export interface LoginRequest {
  email: string;
  password?: string;
}

export interface LoginResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
}

export interface AuthSession {
  accessToken: string;
  expiresAt: string;
  user: AuthUser;
}

export type AuthStateListener = (user: AuthUser | null) => void;

class AuthService {
  private activeSession: AuthSession | null = null;
  private listeners: Set<AuthStateListener> = new Set();
  private isInitialized = false;
  private initializePromise: Promise<AuthUser | null> | null = null;

  constructor() {
    apiClient.setTokenProvider(() => this.getAccessToken());
    apiClient.setRefreshHandler(async () => {
      const session = await this.refresh();
      return session?.accessToken || null;
    });
    apiClient.setSessionExpiredHandler(() => {
      this.clearSession();
      this.notifyListeners(null);
    });
  }

  public getAccessToken(): string | null {
    return this.activeSession?.accessToken || null;
  }

  public getUser(): AuthUser | null {
    return this.activeSession?.user || null;
  }

  public getSession(): AuthSession | null {
    return this.activeSession;
  }

  public isAuthenticated(): boolean {
    return !!this.activeSession?.accessToken;
  }

  public onAuthStateChanged(listener: AuthStateListener): () => void {
    this.listeners.add(listener);
    return () => {
      this.listeners.delete(listener);
    };
  }

  private notifyListeners(user: AuthUser | null): void {
    this.listeners.forEach((listener) => {
      try {
        listener(user);
      } catch {
        // Listener error should not break auth flow
      }
    });
  }

  public setSession(response: LoginResponse): AuthSession {
    const user = parseUserFromToken(response.accessToken);
    if (!user) {
      throw new Error('Geçersiz veya yetkisiz kimlik doğrulama belirteci.');
    }

    const session: AuthSession = {
      accessToken: response.accessToken,
      expiresAt: response.accessTokenExpiresAt,
      user,
    };

    this.activeSession = session;
    this.notifyListeners(user);
    return session;
  }

  public clearSession(): void {
    const hadSession = !!this.activeSession;
    this.activeSession = null;
    if (hadSession) {
      this.notifyListeners(null);
    }
  }

  public async initialize(): Promise<AuthUser | null> {
    if (this.isInitialized && this.activeSession?.user) {
      return this.activeSession.user;
    }

    if (this.initializePromise) {
      return this.initializePromise;
    }

    this.initializePromise = (async () => {
      try {
        // Attempt silent refresh via HttpOnly cookie
        const session = await this.refresh();
        this.isInitialized = true;
        return session?.user || null;
      } catch {
        this.clearSession();
        this.isInitialized = true;
        return null;
      } finally {
        this.initializePromise = null;
      }
    })();

    return this.initializePromise;
  }

  public async login(credentials: LoginRequest): Promise<AuthSession> {
    const response = await apiClient.post<LoginResponse>('/api/v1/auth/login', credentials);
    return this.setSession(response);
  }

  public async refresh(): Promise<AuthSession | null> {
    try {
      const response = await apiClient.post<LoginResponse>('/api/v1/auth/refresh');
      return this.setSession(response);
    } catch {
      this.clearSession();
      return null;
    }
  }

  public async logout(): Promise<void> {
    const token = this.getAccessToken();

    try {
      if (token) {
        await apiClient.post<void>('/api/v1/auth/logout', undefined, { token });
      }
    } catch {
      // Clean up local in-memory session even if backend call fails
    } finally {
      this.clearSession();
    }
  }
}


export const authService = new AuthService();

