import { env } from '@/config/env';
import { ApiError, ProblemDetails } from './apiErrors';

export interface RequestOptions extends Omit<RequestInit, 'body'> {
  params?: Record<string, string | number | boolean | undefined | null>;
  body?: unknown;
  token?: string | null;
  _retry?: boolean;
}

export type TokenProvider = () => string | null;
export type RefreshHandler = () => Promise<string | null>;
export type SessionExpiredHandler = () => void;

class ApiClient {
  private baseUrl: string;
  private tokenProvider: TokenProvider = () => null;
  private refreshHandler: RefreshHandler | null = null;
  private sessionExpiredHandler: SessionExpiredHandler | null = null;
  private refreshPromise: Promise<string | null> | null = null;

  constructor(baseUrl: string) {
    this.baseUrl = baseUrl;
  }

  public setTokenProvider(provider: TokenProvider): void {
    this.tokenProvider = provider;
  }

  public setRefreshHandler(handler: RefreshHandler): void {
    this.refreshHandler = handler;
  }

  public setSessionExpiredHandler(handler: SessionExpiredHandler): void {
    this.sessionExpiredHandler = handler;
  }

  private buildUrl(path: string, params?: Record<string, string | number | boolean | undefined | null>): string {
    const cleanPath = path.startsWith('/') ? path : `/${path}`;
    const url = new URL(`${this.baseUrl}${cleanPath}`);

    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          url.searchParams.append(key, String(value));
        }
      });
    }

    return url.toString();
  }

  private isAuthPath(path: string): boolean {
    const clean = path.toLowerCase();
    return clean.includes('/auth/login') || clean.includes('/auth/refresh') || clean.includes('/auth/logout');
  }

  private async executeSingleFlightRefresh(): Promise<string | null> {
    if (!this.refreshHandler) return null;

    if (!this.refreshPromise) {
      this.refreshPromise = this.refreshHandler().finally(() => {
        this.refreshPromise = null;
      });
    }

    return this.refreshPromise;
  }

  public async request<TResponse>(path: string, options: RequestOptions = {}): Promise<TResponse> {
    const { params, body, headers, token, _retry, ...customConfig } = options;

    const url = this.buildUrl(path, params);
    const activeToken = token !== undefined ? token : this.tokenProvider();

    const requestHeaders: Record<string, string> = {
      'Accept': 'application/json',
      'X-Correlation-Id': crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).substring(2),
    };

    if (activeToken) {
      requestHeaders['Authorization'] = `Bearer ${activeToken}`;
    }

    let requestBody: BodyInit | undefined;
    if (body !== undefined) {
      requestHeaders['Content-Type'] = 'application/json';
      requestBody = typeof body === 'string' ? body : JSON.stringify(body);
    }

    const config: RequestInit = {
      ...customConfig,
      credentials: 'include', // Ensure HttpOnly cookies are transported
      headers: {
        ...requestHeaders,
        ...(headers as Record<string, string>),
      },
      body: requestBody,
    };

    let response: Response;
    try {
      response = await fetch(url, config);
    } catch (networkError) {
      throw new ApiError(
        0,
        networkError instanceof Error ? networkError.message : 'Ağ bağlantısı hatası oluştu.'
      );
    }

    const correlationId = response.headers.get('X-Correlation-Id') || undefined;

    // Handle 401 Unauthorized with single-flight refresh and controlled retry
    if (response.status === 401 && !_retry && !this.isAuthPath(path) && this.refreshHandler) {
      try {
        const newToken = await this.executeSingleFlightRefresh();
        if (newToken) {
          // Retry original request once with new token
          return this.request<TResponse>(path, {
            ...options,
            token: newToken,
            _retry: true,
          });
        }
      } catch {
        // Refresh failed
      }

      // If refresh returned null or failed, trigger session expired
      if (this.sessionExpiredHandler) {
        this.sessionExpiredHandler();
      }
    }

    if (!response.ok) {
      let problem: ProblemDetails | undefined;
      let errorMessage = `HTTP ${response.status}: ${response.statusText}`;

      try {
        const text = await response.text();
        if (text) {
          const parsed = JSON.parse(text);
          if (typeof parsed === 'object' && parsed !== null) {
            problem = parsed as ProblemDetails;
            if (problem.detail) {
              errorMessage = problem.detail;
            } else if (problem.title) {
              errorMessage = problem.title;
            } else if ((parsed as { message?: string }).message) {
              errorMessage = (parsed as { message: string }).message;
            }
          }
        }
      } catch {
        // Fallback to HTTP status text if body is not JSON
      }

      throw new ApiError(response.status, errorMessage, problem, correlationId);
    }

    if (response.status === 204) {
      return undefined as TResponse;
    }

    try {
      const data = await response.json();
      return data as TResponse;
    } catch {
      return undefined as TResponse;
    }
  }

  public get<TResponse>(path: string, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: 'GET' });
  }

  public post<TResponse>(path: string, body?: unknown, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: 'POST', body });
  }

  public put<TResponse>(path: string, body?: unknown, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: 'PUT', body });
  }

  public patch<TResponse>(path: string, body?: unknown, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: 'PATCH', body });
  }

  public delete<TResponse>(path: string, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: 'DELETE' });
  }
}

export const apiClient = new ApiClient(env.apiBaseUrl);

