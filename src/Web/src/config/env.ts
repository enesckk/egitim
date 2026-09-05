export interface AppConfig {
  apiBaseUrl: string;
  isDevelopment: boolean;
  isProduction: boolean;
}

const getApiBaseUrl = (): string => {
  const envUrl = import.meta.env.VITE_API_BASE_URL;

  if (import.meta.env.PROD) {
    if (!envUrl || typeof envUrl !== 'string' || envUrl.trim().length === 0) {
      throw new Error(
        'CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL environment variable is required in production.'
      );
    }
    const trimmed = envUrl.trim().replace(/\/+$/, '');
    if (!trimmed.startsWith('http://') && !trimmed.startsWith('https://')) {
      throw new Error(
        `CRITICAL CONFIGURATION ERROR: VITE_API_BASE_URL "${envUrl}" must be a valid HTTP/HTTPS URL.`
      );
    }
    return trimmed;
  }

  // In development, allow provided envUrl or fallback to local backend default
  if (envUrl && typeof envUrl === 'string' && envUrl.trim().length > 0) {
    return envUrl.trim().replace(/\/+$/, '');
  }
  return 'http://localhost:5000';
};

export const env: AppConfig = {
  apiBaseUrl: getApiBaseUrl(),
  isDevelopment: import.meta.env.DEV,
  isProduction: import.meta.env.PROD,
};

