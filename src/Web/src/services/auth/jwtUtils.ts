import { AuthUser, UserRole } from '@/features/auth/types';

export interface JwtPayload {
  sub?: string;
  nameid?: string;
  email?: string;
  given_name?: string;
  family_name?: string;
  institution_id?: string;
  role?: string | string[];
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'?: string;
  'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'?: string;
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  exp?: number;
  jti?: string;
  [key: string]: unknown;
}

export const decodeJwtPayload = (token: string): JwtPayload | null => {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const base64Url = parts[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );

    return JSON.parse(jsonPayload) as JwtPayload;
  } catch {
    return null;
  }
};

const SUPPORTED_ROLES = ['superadmin', 'institutionadmin', 'admin', 'coach', 'teacher', 'parent', 'student'] as const;

/**
 * Strict role mapping: only explicitly supported backend roles are accepted.
 * Missing, malformed, or unrecognized roles FAIL CLOSED by returning null.
 * 
 * Rules:
 * - If role claim is an array, EVERY element must be a non-empty string and a valid supported role.
 * - If ANY element in the array is invalid (e.g. number, null, empty string, unknown role), the ENTIRE claim is REJECTED (returns null).
 * - Empty arrays return null.
 * - Non-string / non-array claims return null.
 */
export const mapBackendRoleToUserRole = (backendRole?: unknown): UserRole | null => {
  if (backendRole === undefined || backendRole === null) return null;

  let roleList: string[];

  if (Array.isArray(backendRole)) {
    if (backendRole.length === 0) return null;

    // Strict validation: EVERY item in array must be a valid non-empty string
    for (const item of backendRole) {
      if (typeof item !== 'string') {
        return null; // Malformed element (e.g. number, boolean, null) -> fail closed
      }
      const trimmed = item.trim().toLowerCase();
      if (trimmed.length === 0) {
        return null; // Empty string item -> fail closed
      }
      if (!SUPPORTED_ROLES.includes(trimmed as (typeof SUPPORTED_ROLES)[number])) {
        return null; // Unknown role element -> fail closed
      }
    }

    roleList = backendRole.map((r: string) => r.trim().toLowerCase());
  } else if (typeof backendRole === 'string') {
    const trimmed = backendRole.trim().toLowerCase();
    if (trimmed.length === 0) return null;
    if (!SUPPORTED_ROLES.includes(trimmed as (typeof SUPPORTED_ROLES)[number])) {
      return null;
    }
    roleList = [trimmed];
  } else {
    // Malformed type (e.g. number, object, boolean)
    return null;
  }

  // Canonical role precedence mapping
  if (roleList.includes('superadmin') || roleList.includes('institutionadmin') || roleList.includes('admin')) {
    return 'admin';
  }
  if (roleList.includes('coach')) {
    return 'coach';
  }
  if (roleList.includes('teacher')) {
    return 'teacher';
  }
  if (roleList.includes('parent')) {
    return 'parent';
  }
  if (roleList.includes('student')) {
    return 'student';
  }

  // Fail closed
  return null;
};

export const getRoleDisplayName = (role: UserRole): string => {
  switch (role) {
    case 'student':
      return 'Öğrenci';
    case 'coach':
      return 'YKS Koçu';
    case 'teacher':
      return 'Öğretmen';
    case 'parent':
      return 'Veli';
    case 'admin':
      return 'Kurum Yöneticisi';
  }
};

export const parseUserFromToken = (token: string): AuthUser | null => {
  const payload = decodeJwtPayload(token);
  if (!payload) return null;

  // P2: Reject expired JWT tokens immediately
  if (typeof payload.exp === 'number') {
    const expiresAtMs = payload.exp * 1000;
    if (expiresAtMs <= Date.now()) {
      return null;
    }
  }

  const rawRole =
    payload.role ||
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

  const role = mapBackendRoleToUserRole(rawRole);
  if (!role) {
    // Fail closed if role is invalid, unknown or missing
    return null;
  }

  const id =
    payload.sub ||
    payload.nameid ||
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];

  if (!id) {
    return null;
  }

  const email =
    payload.email ||
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] ||
    '';

  const firstName =
    payload.given_name ||
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname'] ||
    '';

  const lastName =
    payload.family_name ||
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname'] ||
    '';

  const fullName = firstName && lastName ? `${firstName} ${lastName}` : email.split('@')[0] || 'Kullanıcı';

  const initials = fullName
    .split(' ')
    .filter(Boolean)
    .map((n) => n[0])
    .slice(0, 2)
    .join('')
    .toUpperCase() || 'BA';

  const institutionId = payload.institution_id || undefined;

  return {
    id,
    name: fullName,
    email,
    role,
    initials,
    roleLabel: getRoleDisplayName(role),
    institutionName: institutionId ? 'Bağlı Kurum' : 'Bilim Akademi',
  };
};

