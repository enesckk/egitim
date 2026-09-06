import { test, expect } from '@playwright/test';
import { validateAndNormalizeApiBaseUrl } from '../src/config/env';
import { mapBackendRoleToUserRole, parseUserFromToken } from '../src/services/auth/jwtUtils';
import { AuthService } from '../src/services/auth/authService';

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

test.describe('P1-01: Session Lifecycle & Generation Invalidation Deterministic Tests', () => {
  test('1. Logout occurs while refresh is in-flight -> refresh resolution must NOT re-authenticate user', async () => {
    const auth = new AuthService();
    const token = createMockJwt({ sub: 'usr-refresh', email: 'refresh@example.com', role: 'Student' });

    // Simulate in-flight refresh with captured generation
    const refreshGen = auth.getSessionGeneration();

    // User logs out before refresh completes
    await auth.logout();
    expect(auth.getUser()).toBeNull();
    expect(auth.isAuthenticated()).toBe(false);

    // Stale refresh response arrives afterward
    const staleResult = auth.setSession(
      { accessToken: token, accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString() },
      refreshGen // stale generation
    );

    expect(staleResult).toBeNull();
    expect(auth.getUser()).toBeNull();
    expect(auth.isAuthenticated()).toBe(false);
  });

  test('2. Normal valid refresh establishes active session', () => {
    const auth = new AuthService();
    const token = createMockJwt({ sub: 'usr-valid', email: 'valid@example.com', role: 'Coach' });
    const currentGen = auth.getSessionGeneration();

    const session = auth.setSession(
      { accessToken: token, accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString() },
      currentGen
    );

    expect(session).not.toBeNull();
    expect(auth.isAuthenticated()).toBe(true);
    expect(auth.getUser()?.role).toBe('coach');
  });

  test('3. Stale refresh does not overwrite a newer session', () => {
    const auth = new AuthService();
    const user1Token = createMockJwt({ sub: 'usr-1', email: 'u1@example.com', role: 'Student' });
    const user2Token = createMockJwt({ sub: 'usr-2', email: 'u2@example.com', role: 'Teacher' });

    // Refresh was started during generation 0
    const gen1 = auth.getSessionGeneration();

    // User performs login with new credentials (login advances generation to 1)
    (auth as unknown as { sessionGeneration: number }).sessionGeneration++;
    const gen2 = auth.getSessionGeneration();

    auth.setSession(
      { accessToken: user2Token, accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString() },
      gen2
    );
    expect(auth.getUser()?.role).toBe('teacher');

    // Stale gen1 refresh response arrives afterward
    const staleResult = auth.setSession(
      { accessToken: user1Token, accessTokenExpiresAt: new Date(Date.now() + 3600000).toISOString() },
      gen1
    );

    expect(staleResult).toBeNull();
    // User remains Teacher, not overwritten by Student
    expect(auth.getUser()?.role).toBe('teacher');
  });
});
