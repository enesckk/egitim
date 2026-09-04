export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly problem?: ProblemDetails;
  public readonly validationErrors?: Record<string, string[]>;
  public readonly correlationId?: string;

  constructor(status: number, message: string, problem?: ProblemDetails, correlationId?: string) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
    this.correlationId = correlationId;

    if (problem?.errors) {
      this.validationErrors = problem.errors;
    }
  }

  get isNetworkError(): boolean {
    return this.status === 0;
  }

  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  get isForbidden(): boolean {
    return this.status === 403;
  }

  get isNotFound(): boolean {
    return this.status === 404;
  }

  get isConflict(): boolean {
    return this.status === 409;
  }

  get isRateLimited(): boolean {
    return this.status === 429;
  }

  get isValidationError(): boolean {
    return this.status === 400 && !!this.validationErrors;
  }

  get isServerError(): boolean {
    return this.status >= 500;
  }

  /**
   * Returns a user-safe localized description of the error.
   */
  getUserMessage(): string {
    if (this.isNetworkError) {
      return 'Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol ediniz.';
    }
    if (this.isRateLimited) {
      return 'Çok fazla istek gönderildi. Lütfen bir süre bekleyip tekrar deneyiniz.';
    }
    if (this.isUnauthorized) {
      return 'Giriş bilgileri geçersiz veya oturum süreniz doldu.';
    }
    if (this.isForbidden) {
      return 'Bu işlemi gerçekleştirmek için gerekli yetkiniz bulunmuyor.';
    }
    if (this.isNotFound) {
      return 'İstenen kayıt veya sayfa bulunamadı.';
    }
    if (this.isConflict) {
      return this.problem?.detail || 'Bu işlem mevcut verilerle çakışıyor.';
    }
    if (this.isValidationError && this.validationErrors) {
      const firstKey = Object.keys(this.validationErrors)[0];
      if (firstKey && this.validationErrors[firstKey]?.length) {
        return this.validationErrors[firstKey][0];
      }
    }
    if (this.problem?.detail && this.status < 500) {
      return this.problem.detail;
    }
    return 'Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyiniz.';
  }
}

