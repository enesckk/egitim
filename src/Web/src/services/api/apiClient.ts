import { env } from '@/config/env';
import { ApiError, ProblemDetails } from './apiErrors';

export interface RequestOptions extends Omit<RequestInit, 'body'> {
  params?: Record<string, string | number | boolean | undefined | null>;
  body?: unknown;
  token?: string | null;
  _retry?: boolean;
}

export type RefreshResult =
  | { status: 'refreshed'; accessToken: string }
  | { status: 'stale' }
  | { status: 'unauthenticated' };

export type TokenProvider = () => string | null;
export type RefreshHandler = () => Promise<RefreshResult>;
export type SessionExpiredHandler = () => void;

class ApiClient {
  private baseUrl: string;
  private tokenProvider: TokenProvider = () => null;
  private refreshHandler: RefreshHandler | null = null;
  private sessionExpiredHandler: SessionExpiredHandler | null = null;
  private refreshPromise: Promise<RefreshResult> | null = null;

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
    
    // Support same-origin reverse proxy paths (e.g. baseUrl is "" or starts with "/")
    let fullPath: string;
    if (!this.baseUrl || this.baseUrl === '') {
      fullPath = cleanPath;
    } else if (this.baseUrl.startsWith('/')) {
      const normalizedBase = this.baseUrl.replace(/\/+$/, '');
      fullPath = `${normalizedBase}${cleanPath}`;
    } else {
      fullPath = `${this.baseUrl.replace(/\/+$/, '')}${cleanPath}`;
    }

    // In browser environments with relative fullPath, use window.location.origin as base
    const baseOrigin = (typeof window !== 'undefined' && window.location?.origin)
      ? window.location.origin
      : 'http://localhost';

    const url = fullPath.startsWith('http://') || fullPath.startsWith('https://')
      ? new URL(fullPath)
      : new URL(fullPath, baseOrigin);

    if (params) {
      Object.entries(params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          url.searchParams.append(key, String(value));
        }
      });
    }

    // If original baseUrl was relative, return pathname + search
    if (!this.baseUrl || this.baseUrl.startsWith('/')) {
      return `${url.pathname}${url.search}`;
    }

    return url.toString();
  }

  private isAuthPath(path: string): boolean {
    const clean = path.toLowerCase();
    return clean.includes('/auth/login') || clean.includes('/auth/refresh') || clean.includes('/auth/logout');
  }

  private async executeSingleFlightRefresh(): Promise<RefreshResult> {
    if (!this.refreshHandler) return { status: 'unauthenticated' };

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
      if (body instanceof FormData) {
        requestBody = body;
      } else {
        requestHeaders['Content-Type'] = 'application/json';
        requestBody = typeof body === 'string' ? body : JSON.stringify(body);
      }
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
        const refreshResult = await this.executeSingleFlightRefresh();
        if (refreshResult.status === 'refreshed') {
          // Retry original request once with new token
          return this.request<TResponse>(path, {
            ...options,
            token: refreshResult.accessToken,
            _retry: true,
          });
        } else if (refreshResult.status === 'stale') {
          // Stale refresh result from an older session generation:
          // NEVER clear the new session, NEVER call sessionExpiredHandler.
          throw new ApiError(401, 'Eski oturum yenileme isteği geçersiz kılındı.');
        } else if (refreshResult.status === 'unauthenticated') {
          // Real refresh failure belonging to current session
          if (this.sessionExpiredHandler) {
            this.sessionExpiredHandler();
          }
        }
      } catch (err) {
        if (err instanceof ApiError) {
          throw err;
        }
        if (this.sessionExpiredHandler) {
          this.sessionExpiredHandler();
        }
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

  public postForm<TResponse>(path: string, formData: FormData, options?: RequestOptions): Promise<TResponse> {
    return this.request<TResponse>(path, { ...options, method: 'POST', body: formData });
  }

  public async getBlob(path: string, options?: RequestOptions): Promise<Blob> {
    const url = this.buildUrl(path, options?.params);
    const activeToken = options?.token !== undefined ? options.token : this.tokenProvider();

    const requestHeaders: Record<string, string> = {
      'Accept': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet, application/octet-stream, */*',
    };

    if (activeToken) {
      requestHeaders['Authorization'] = `Bearer ${activeToken}`;
    }

    const response = await fetch(url, {
      method: 'GET',
      credentials: 'include',
      headers: {
        ...requestHeaders,
        ...(options?.headers as Record<string, string>),
      },
    });

    if (!response.ok) {
      throw new ApiError(response.status, `HTTP ${response.status}: Şablon dosyası indirilemedi.`);
    }

    return response.blob();
  }
}

export const apiClient = new ApiClient(env.apiBaseUrl);

