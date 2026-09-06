import { test, expect } from '@playwright/test';
import { validateAndNormalizeApiBaseUrl } from '../src/config/env';
import { mapBackendRoleToUserRole, parseUserFromToken } from '../src/services/auth/jwtUtils';
import { AuthService } from '../src/services/auth/authService';
import { apiClient } from '../src/services/api/apiClient';

function createMockJwt(payload: Record<string, unknown>, expSeconds = 3600): string {
  const header = Buffer.from(JSON.stringify({ alg: 'HS256', typ: 'JWT' })).toString('base64url');
  const exp = Math.floor(Date.now() / 1000) + expSeconds;
  const body = Buffer.from(JSON.stringify({ exp, ...payload })).toString('base64url');
  return `${header}.${body}.mock_signature`;
}

test.describe('P2-01: API Base URL Validation Tests', () => {
  test('Accepts valid absolute URLs and normalizes trailing slashes', () => {
    expect(validateAndNormalizeApiBaseUrl('https://api.example.com')).toBe('https://api.example.com');
    expect(validateAndNormalizeApiBaseUrl('https://api.example.com/')).toBe('https://api.example.com');
    expect(validateAndNormalizeApiBaseUrl('https://api.example.com/api')).toBe('https://api.example.com/api');
    expect(validateAndNormalizeApiBaseUrl('https://api.example.com/api/')).toBe('https://api.example.com/api');
    expect(validateAndNormalizeApiBaseUrl('http://localhost:5000')).toBe('http://localhost:5000');
    expect(validateAndNormalizeApiBaseUrl('http://localhost:5000/')).toBe('http://localhost:5000');
  });

  test('Rejects malformed host, not-a-url, and invalid schemes', () => {
    expect(() => validateAndNormalizeApiBaseUrl('https://bad host')).toThrow();
    expect(() => validateAndNormalizeApiBaseUrl('not-a-url')).toThrow();
    expect(() => validateAndNormalizeApiBaseUrl('ftp://example.com')).toThrow();
    expect(() => validateAndNormalizeApiBaseUrl('javascript:alert(1)')).toThrow();
  });

  test('Rejects credentials, query strings, and hash fragments', () => {
    expect(() => validateAndNormalizeApiBaseUrl('https://user:pass@example.com')).toThrow(/credentials/i);
    expect(() => validateAndNormalizeApiBaseUrl('https://api.example.com?x=1')).toThrow(/query/i);
    expect(() => validateAndNormalizeApiBaseUrl('https://api.example.com#fragment')).toThrow(/hash|fragment/i);
  });

  test('Development fallback to localhost vs Production mandatory failure', () => {
    // Dev: fallback
    expect(validateAndNormalizeApiBaseUrl('', false)).toBe('http://localhost:5000');
    expect(validateAndNormalizeApiBaseUrl(undefined, false)).toBe('http://localhost:5000');

    // Prod: must throw
    expect(() => validateAndNormalizeApiBaseUrl('', true)).toThrow(/required in production/i);
    expect(() => validateAndNormalizeApiBaseUrl(undefined, true)).toThrow(/required in production/i);
    expect(() => validateAndNormalizeApiBaseUrl('   ', true)).toThrow(/required in production/i);
  });
});

test.describe('P2-03: Strict Role Claim Validation Tests (Fail Closed)', () => {
  test('Accepts valid supported roles', () => {
    expect(mapBackendRoleToUserRole('Student')).toBe('student');
    expect(mapBackendRoleToUserRole('student')).toBe('student');
    expect(mapBackendRoleToUserRole(['Student'])).toBe('student');
    expect(mapBackendRoleToUserRole('Coach')).toBe('coach');
    expect(mapBackendRoleToUserRole(['Coach'])).toBe('coach');
    expect(mapBackendRoleToUserRole('Teacher')).toBe('teacher');
    expect(mapBackendRoleToUserRole(['Teacher'])).toBe('teacher');
    expect(mapBackendRoleToUserRole('Parent')).toBe('parent');
    expect(mapBackendRoleToUserRole(['Parent'])).toBe('parent');
    expect(mapBackendRoleToUserRole('InstitutionAdmin')).toBe('admin');
    expect(mapBackendRoleToUserRole(['InstitutionAdmin'])).toBe('admin');
    expect(mapBackendRoleToUserRole('SuperAdmin')).toBe('admin');
    expect(mapBackendRoleToUserRole(['SuperAdmin'])).toBe('admin');
    expect(mapBackendRoleToUserRole(['InstitutionAdmin', 'Teacher'])).toBe('admin');
  });

  test('Rejects malformed array elements without silently filtering them', () => {
    // ["Student", 42] must FAIL CLOSED, not silently accept Student
    expect(mapBackendRoleToUserRole(['Student', 42])).toBeNull();
    expect(mapBackendRoleToUserRole(['Student', null])).toBeNull();
    expect(mapBackendRoleToUserRole(['Student', undefined])).toBeNull();
    expect(mapBackendRoleToUserRole(['Student', 'UnknownRole'])).toBeNull();
    expect(mapBackendRoleToUserRole(['Student', ''])).toBeNull();
    expect(mapBackendRoleToUserRole(['Student', '   '])).toBeNull();
  });

  test('Rejects empty array, numbers, objects, and unknown strings', () => {
    expect(mapBackendRoleToUserRole([])).toBeNull();
    expect(mapBackendRoleToUserRole([42])).toBeNull();
    expect(mapBackendRoleToUserRole('UnknownRole')).toBeNull();
    expect(mapBackendRoleToUserRole('')).toBeNull();
    expect(mapBackendRoleToUserRole(null)).toBeNull();
    expect(mapBackendRoleToUserRole(undefined)).toBeNull();
    expect(mapBackendRoleToUserRole(123)).toBeNull();
    expect(mapBackendRoleToUserRole({ role: 'Student' })).toBeNull();
  });

  test('parseUserFromToken rejects malformed/unknown role and expired tokens', () => {
    const validStudentToken = createMockJwt({
      sub: 'usr-1',
      email: 'student@example.com',
      role: 'Student',
    });
    const parsedValid = parseUserFromToken(validStudentToken);
    expect(parsedValid).not.toBeNull();
    expect(parsedValid?.role).toBe('student');

    const malformedRoleToken = createMockJwt({
      sub: 'usr-2',
      email: 'bad@example.com',
      role: ['Student', 42],
    });
    expect(parseUserFromToken(malformedRoleToken)).toBeNull();

    const expiredToken = createMockJwt(
      {
        sub: 'usr-3',
        email: 'expired@example.com',
        role: 'Student',
      },
      -100 // expired 100 seconds ago
    );
    expect(parseUserFromToken(expiredToken)).toBeNull();
  });
});

test.describe('P1-01 & P2-01: Session Lifecycle & Race Condition Public Flow Tests', () => {
  test('Scenario 1: Old API request -> 401 -> refresh starts -> new login succeeds -> old refresh resolves -> new user remains authenticated', async () => {
    const auth = new AuthService();
    const newUserToken = createMockJwt({ sub: 'usr-new', email: 'teacher@example.com', role: 'Teacher' });
    const staleRefreshToken = createMockJwt({ sub: 'usr-stale', email: 'student@example.com', role: 'Student' });

    // Step 1: User is logged in as Student
    auth.setSession({
      accessToken: createMockJwt({ sub: 'usr-stale', email: 'student@example.com', role: 'Student' }),
      accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
    });
    expect(auth.getUser()?.role).toBe('student');

    // Step 2: Refresh starts, holding response
    let resolveRefresh: ((res: { accessToken: string; accessTokenExpiresAt: string }) => void) | null = null;
    const refreshPromise = new Promise<{ accessToken: string; accessTokenExpiresAt: string }>((resolve) => {
      resolveRefresh = resolve;
    });

    // Mock apiClient post for refresh
    const origPost = apiClient.post.bind(apiClient);
    let refreshTriggered = false;
    apiClient.post = async <T>(path: string, body?: unknown, options?: import('../src/services/api/apiClient').RequestOptions): Promise<T> => {
      if (path === '/api/v1/auth/refresh') {
        refreshTriggered = true;
        const res = await refreshPromise;
        return res as T;
      }
      if (path === '/api/v1/auth/login') {
        return {
          accessToken: newUserToken,
          accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
        } as T;
      }
      return origPost(path, body, options);
    };

    // Start background refresh
    const inFlightRefresh = auth.refresh();
    expect(refreshTriggered).toBe(true);

    // Step 3: While refresh is in-flight, user performs a new login
    await auth.login({ email: 'teacher@example.com', password: 'Password123!' });
    expect(auth.getUser()?.role).toBe('teacher');

    // Step 4: Old refresh finally resolves
    if (resolveRefresh) {
      (resolveRefresh as (res: { accessToken: string; accessTokenExpiresAt: string }) => void)({
        accessToken: staleRefreshToken,
        accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
      });
    }
    const refreshResult = await inFlightRefresh;

    // Step 5: Refresh outcome MUST be stale, and new user MUST remain authenticated as Teacher
    expect(refreshResult.status).toBe('stale');
    expect(auth.getUser()?.role).toBe('teacher');
    expect(auth.isAuthenticated()).toBe(true);

    apiClient.post = origPost;
  });

  test('Scenario 2: Old API request -> 401 -> refresh starts -> new login succeeds -> old refresh FAILS -> new user remains authenticated', async () => {
    const auth = new AuthService();
    const newUserToken = createMockJwt({ sub: 'usr-coach', email: 'coach@example.com', role: 'Coach' });

    // Step 1: User is logged in as Student
    auth.setSession({
      accessToken: createMockJwt({ sub: 'usr-student', email: 'student@example.com', role: 'Student' }),
      accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
    });

    // Step 2: Refresh starts, holding failure
    let rejectRefresh: ((err: Error) => void) | null = null;
    const refreshPromise = new Promise<{ accessToken: string; accessTokenExpiresAt: string }>((_, reject) => {
      rejectRefresh = reject;
    });

    const origPost = apiClient.post.bind(apiClient);
    let refreshTriggered = false;
    apiClient.post = async <T>(path: string, body?: unknown, options?: import('../src/services/api/apiClient').RequestOptions): Promise<T> => {
      if (path === '/api/v1/auth/refresh') {
        refreshTriggered = true;
        await refreshPromise;
        return undefined as T;
      }
      if (path === '/api/v1/auth/login') {
        return {
          accessToken: newUserToken,
          accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
        } as T;
      }
      return origPost(path, body, options);
    };

    const inFlightRefresh = auth.refresh();
    expect(refreshTriggered).toBe(true);

    // Step 3: New login succeeds
    await auth.login({ email: 'coach@example.com', password: 'Password123!' });
    expect(auth.getUser()?.role).toBe('coach');

    // Step 4: Old refresh fails (e.g. 401 / network error)
    if (rejectRefresh) {
      (rejectRefresh as (err: Error) => void)(new Error('Network error / 401 Unauthorized'));
    }
    const refreshResult = await inFlightRefresh;

    // Step 5: Refresh failure is recognized as stale, and Coach session is NOT cleared
    expect(refreshResult.status).toBe('stale');
    expect(auth.getUser()?.role).toBe('coach');
    expect(auth.isAuthenticated()).toBe(true);

    apiClient.post = origPost;
  });

  test('Scenario 3: Refresh starts -> logout occurs -> old refresh succeeds -> user remains logged out', async () => {
    const auth = new AuthService();
    const token = createMockJwt({ sub: 'usr-student', email: 'student@example.com', role: 'Student' });

    let resolveRefresh: ((res: { accessToken: string; accessTokenExpiresAt: string }) => void) | null = null;
    const refreshPromise = new Promise<{ accessToken: string; accessTokenExpiresAt: string }>((resolve) => {
      resolveRefresh = resolve;
    });

    const origPost = apiClient.post.bind(apiClient);
    let refreshTriggered = false;
    apiClient.post = async <T>(path: string, body?: unknown, options?: import('../src/services/api/apiClient').RequestOptions): Promise<T> => {
      if (path === '/api/v1/auth/refresh') {
        refreshTriggered = true;
        const res = await refreshPromise;
        return res as T;
      }
      if (path === '/api/v1/auth/logout') {
        return undefined as T;
      }
      return origPost(path, body, options);
    };

    // Refresh starts
    const inFlightRefresh = auth.refresh();
    expect(refreshTriggered).toBe(true);

    // User logs out before refresh finishes
    await auth.logout();
    expect(auth.getUser()).toBeNull();
    expect(auth.isAuthenticated()).toBe(false);

    // Refresh resolves afterward
    if (resolveRefresh) {
      (resolveRefresh as (res: { accessToken: string; accessTokenExpiresAt: string }) => void)({
        accessToken: token,
        accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
      });
    }
    const refreshResult = await inFlightRefresh;

    // User MUST remain logged out
    expect(refreshResult.status).toBe('stale');
    expect(auth.getUser()).toBeNull();
    expect(auth.isAuthenticated()).toBe(false);

    apiClient.post = origPost;
  });

  test('Scenario 4: Concurrent initialize calls trigger exactly one refresh request', async () => {
    const auth = new AuthService();
    const token = createMockJwt({ sub: 'usr-init', email: 'init@example.com', role: 'Student' });

    let refreshCount = 0;
    const origPost = apiClient.post.bind(apiClient);
    apiClient.post = async <T>(path: string, body?: unknown, options?: import('../src/services/api/apiClient').RequestOptions): Promise<T> => {
      if (path === '/api/v1/auth/refresh') {
        refreshCount++;
        return {
          accessToken: token,
          accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString(),
        } as T;
      }
      return origPost(path, body, options);
    };

    // Concurrent initialize calls
    const [user1, user2, user3] = await Promise.all([
      auth.initialize(),
      auth.initialize(),
      auth.initialize(),
    ]);

    expect(refreshCount).toBe(1);
    expect(user1?.role).toBe('student');
    expect(user2?.role).toBe('student');
    expect(user3?.role).toBe('student');

    apiClient.post = origPost;
  });
});
