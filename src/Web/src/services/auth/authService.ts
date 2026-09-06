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

export class AuthService {
  private activeSession: AuthSession | null = null;
  private listeners: Set<AuthStateListener> = new Set();
  private isInitialized = false;
  private initializePromise: Promise<AuthUser | null> | null = null;
  private sessionGeneration = 0;

  constructor() {
    apiClient.setTokenProvider(() => this.getAccessToken());
    apiClient.setRefreshHandler(async () => {
      const session = await this.refresh();
      return session?.accessToken || null;
    });
    apiClient.setSessionExpiredHandler(() => {
      this.sessionGeneration++;
      this.clearSession();
      this.notifyListeners(null);
    });
  }

  public getSessionGeneration(): number {
    return this.sessionGeneration;
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

  /**
   * Sets the active session. If an expectedGeneration is provided,
   * rejects setting the session if the generation has moved (e.g. stale response after logout).
   */
  public setSession(response: LoginResponse, expectedGeneration?: number): AuthSession | null {
    if (expectedGeneration !== undefined && expectedGeneration !== this.sessionGeneration) {
      // Generation mismatch: this response belongs to a cancelled/stale session lifecycle
      return null;
    }

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

    const initGen = this.sessionGeneration;

    this.initializePromise = (async () => {
      try {
        // Attempt silent refresh via HttpOnly cookie
        const session = await this.refresh();
        if (initGen !== this.sessionGeneration) {
          // Logged out or session generation advanced while initialize was in-flight
          return null;
        }
        this.isInitialized = true;
        return session?.user || null;
      } catch {
        if (initGen === this.sessionGeneration) {
          this.clearSession();
          this.isInitialized = true;
        }
        return null;
      } finally {
        this.initializePromise = null;
      }
    })();

    return this.initializePromise;
  }

  public async login(credentials: LoginRequest): Promise<AuthSession> {
    // Increment generation so any older in-flight refresh or request cannot overwrite new login
    this.sessionGeneration++;
    const currentGen = this.sessionGeneration;

    const response = await apiClient.post<LoginResponse>('/api/v1/auth/login', credentials);

    if (currentGen !== this.sessionGeneration) {
      throw new Error('Oturum başlatma işlemi iptal edildi.');
    }

    const session = this.setSession(response, currentGen);
    if (!session) {
      throw new Error('Oturum oluşturulamadı.');
    }
    return session;
  }

  public async refresh(): Promise<AuthSession | null> {
    const currentGen = this.sessionGeneration;

    try {
      const response = await apiClient.post<LoginResponse>('/api/v1/auth/refresh');
      if (currentGen !== this.sessionGeneration) {
        // Stale refresh response: user logged out or session invalidated while request was in-flight
        return null;
      }
      return this.setSession(response, currentGen);
    } catch {
      if (currentGen === this.sessionGeneration) {
        this.clearSession();
      }
      return null;
    }
  }

  public async logout(): Promise<void> {
    // 1. Invalidate session generation BEFORE any async operation or cleanup
    this.sessionGeneration++;
    this.initializePromise = null;

    const token = this.getAccessToken();

    // 2. Clear local session immediately so UI is logged out synchronously
    this.clearSession();

    // 3. Notify backend revocation
    try {
      if (token) {
        await apiClient.post<void>('/api/v1/auth/logout', undefined, { token });
      }
    } catch {
      // Local session already safely cleared
    }
  }
}

export const authService = new AuthService();

