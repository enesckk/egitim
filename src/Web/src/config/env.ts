export interface AppConfig {
  apiBaseUrl: string;
  isDevelopment: boolean;
  isProduction: boolean;
}

export interface ResolveApiBaseUrlParams {
  configuredValue?: string;
  isProduction?: boolean;
  isDevelopment?: boolean;
}

/**
 * Validates and normalizes the API base URL.
 * 
 * Rules:
 * - Production: configuredValue is required and cannot fall back to localhost.
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
export const resolveApiBaseUrl = ({
  configuredValue,
  isProduction = false,
}: ResolveApiBaseUrlParams): string => {
  const trimmed = configuredValue ? configuredValue.trim() : '';

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
      `CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL "${configuredValue}" is not a valid absolute URL.`
    );
  }

  // 1. Protocol check
  if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
    throw new Error(
      `CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL "${configuredValue}" must use http or https protocol.`
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
      `CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL "${configuredValue}" has an invalid host.`
    );
  }

  // Centrally normalize: origin + pathname with trailing slashes removed
  const normalizedPath = parsed.pathname.replace(/\/+$/, '');
  return `${parsed.origin}${normalizedPath}`;
};

// Export alias for helper testing
export const validateAndNormalizeApiBaseUrl = (
  rawUrl?: string,
  isProduction: boolean = false
): string => resolveApiBaseUrl({ configuredValue: rawUrl, isProduction });

// Initialize import.meta.env for Node test environments if not defined by runtime
if (typeof import.meta.env === 'undefined') {
  (import.meta as unknown as { env: Record<string, unknown> }).env = {
    DEV: true,
    PROD: false,
    VITE_API_BASE_URL: typeof process !== 'undefined' ? process.env.VITE_API_BASE_URL : undefined,
  };
}

// Direct Vite compile-time environment access:
// Vite replaces import.meta.env.* statically during production build.
export const env: AppConfig = {
  apiBaseUrl: resolveApiBaseUrl({
    configuredValue: import.meta.env.VITE_API_BASE_URL,
    isProduction: import.meta.env.PROD,
    isDevelopment: import.meta.env.DEV,
  }),
  isDevelopment: import.meta.env.DEV,
  isProduction: import.meta.env.PROD,
};

