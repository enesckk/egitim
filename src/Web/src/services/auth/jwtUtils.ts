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

/**
 * Strict role mapping: only explicitly supported backend roles are accepted.
 * Missing, malformed, or unrecognized roles FAIL CLOSED by returning null.
 */
export const mapBackendRoleToUserRole = (backendRole?: string | string[]): UserRole | null => {
  if (!backendRole) return null;
  const roles = Array.isArray(backendRole) ? backendRole : [backendRole];
  const normalized = roles.map((r) => (typeof r === 'string' ? r.toLowerCase().trim() : '')).filter(Boolean);

  if (normalized.length === 0) return null;

  if (normalized.includes('superadmin') || normalized.includes('institutionadmin') || normalized.includes('admin')) {
    return 'admin';
  }
  if (normalized.includes('coach')) {
    return 'coach';
  }
  if (normalized.includes('teacher')) {
    return 'teacher';
  }
  if (normalized.includes('parent')) {
    return 'parent';
  }
  if (normalized.includes('student')) {
    return 'student';
  }

  // Fail closed: Unknown role must NEVER fall back to student or any default role
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

