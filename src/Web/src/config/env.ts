export interface AppConfig {
  apiBaseUrl: string;
  isDevelopment: boolean;
  isProduction: boolean;
}

/**
 * Validates and normalizes the API base URL.
 * 
 * Rules:
 * - Production: VITE_API_BASE_URL is mandatory and cannot fall back to localhost.
 * - Development: Falls back to http://localhost:5000 if not provided.
 * - If a URL is provided, it MUST:
 *   - Parse as a valid absolute URL using the standard URL constructor.
 *   - Use 'http:' or 'https:' protocol.
 *   - Contain NO credentials (username/password).
 *   - Contain NO query string parameters.
 *   - Contain NO hash/fragments.
 *   - Have a non-empty, valid hostname with no whitespace.
 * - Trailing slashes are centrally stripped.
 */
export const validateAndNormalizeApiBaseUrl = (
  rawUrl?: string,
  isProduction: boolean = false
): string => {
  const trimmed = rawUrl ? rawUrl.trim() : '';

  if (!trimmed) {
    if (isProduction) {
      throw new Error(
        'CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL environment variable is required in production.'
      );
    }
    return 'http://localhost:5000';
  }

  let parsed: URL;
  try {
    parsed = new URL(trimmed);
  } catch {
    throw new Error(
      `CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL "${rawUrl}" is not a valid absolute URL.`
    );
  }

  // 1. Protocol check
  if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
    throw new Error(
      `CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL "${rawUrl}" must use http or https protocol.`
    );
  }

  // 2. Reject credentials
  if (parsed.username || parsed.password) {
    throw new Error(
      'CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL must not contain credentials.'
    );
  }

  // 3. Reject query string
  if (parsed.search && parsed.search.length > 0) {
    throw new Error(
      'CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL must not contain query parameters.'
    );
  }

  // 4. Reject hash / fragment
  if (parsed.hash && parsed.hash.length > 0) {
    throw new Error(
      'CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL must not contain a URL hash/fragment.'
    );
  }

  // 5. Host check
  if (!parsed.hostname || parsed.hostname.includes(' ')) {
    throw new Error(
      `CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL "${rawUrl}" has an invalid host.`
    );
  }

  // Centrally normalize: origin + pathname with trailing slashes removed
  const normalizedPath = parsed.pathname.replace(/\/+$/, '');
  return `${parsed.origin}${normalizedPath}`;
};

// Safely access import.meta.env in both Vite bundler and Node test runners
const metaEnv =
  typeof import.meta !== 'undefined' && 'env' in import.meta
    ? (import.meta as { env: Record<string, string | boolean | undefined> }).env
    : {};

export const env: AppConfig = {
  apiBaseUrl: validateAndNormalizeApiBaseUrl(
    metaEnv.VITE_API_BASE_URL as string | undefined,
    Boolean(metaEnv.PROD)
  ),
  isDevelopment: Boolean(metaEnv.DEV ?? true),
  isProduction: Boolean(metaEnv.PROD),
};

