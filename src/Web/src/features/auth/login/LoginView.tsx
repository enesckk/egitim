import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { Mail, Lock, Eye, EyeOff, Layers, AlertCircle, AlertTriangle, WifiOff } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/auth/useAuth';
import { getRoleDefaultRoute } from '@/auth/roleRoutes';
import { ApiError } from '@/services/api';

export const LoginView: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, isLoading } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errorType, setErrorType] = useState<'auth' | 'rate-limit' | 'network' | 'validation' | null>(null);

  const fromLocation = (location.state as { from?: { pathname: string } })?.from?.pathname;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isLoading) return;

    const trimmedEmail = email.trim();
    if (!trimmedEmail || !password) {
      setErrorType('validation');
      setErrorMessage('Lütfen e-posta adresinizi ve şifrenizi giriniz.');
      return;
    }

    setErrorType(null);
    setErrorMessage(null);

    try {
      // P1-1 Closure: Use returned authenticated user directly to avoid React state race/stale closures
      const authenticatedUser = await login({ emailOrUsername: trimmedEmail, password });

      // Determine target destination strictly from returned authenticated session
      if (fromLocation && fromLocation !== '/login' && fromLocation !== '/403') {
        navigate(fromLocation, { replace: true });
      } else {
        navigate(getRoleDefaultRoute(authenticatedUser.role), { replace: true });
      }
    } catch (err: unknown) {
      if (err instanceof ApiError) {
        if (err.isRateLimited) {
          setErrorType('rate-limit');
          setErrorMessage('Çok fazla başarısız giriş denemesi yapıldı. Lütfen birkaç dakika sonra tekrar deneyiniz.');
        } else if (err.isNetworkError) {
          setErrorType('network');
          setErrorMessage('Sunucuya bağlanılamadı. Lütfen internet bağlantınızı kontrol ediniz.');
        } else if (err.isUnauthorized || err.status === 400) {
          setErrorType('auth');
          setErrorMessage('E-posta adresi veya şifre hatalı. Lütfen bilgilerinizi kontrol ediniz.');
        } else {
          setErrorType('auth');
          setErrorMessage(err.getUserMessage());
        }
      } else if (err instanceof Error && err.message) {
        setErrorType('auth');
        setErrorMessage(err.message);
      } else {
        setErrorType('auth');
        setErrorMessage('Giriş yapılırken beklenmeyen bir hata oluştu. Lütfen tekrar deneyiniz.');
      }
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface px-4 py-8 sm:px-6 lg:px-8 select-none">
      <div className="w-full max-w-md">
        {/* Institutional Brand Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-navy-900 text-white shadow-soft-sm mb-4">
            <Layers className="h-6 w-6 text-primary-400" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-navy-900 font-sans">
            Bilim Akademi
          </h1>
          <p className="text-sm text-neutral-500 mt-1">
            Eğitim Yönetim ve Takip Platformu
          </p>
        </div>

        {/* Login Card */}
        <div className="bg-white rounded-2xl border border-neutral-200 shadow-soft-sm p-6 sm:p-8">
          <div className="mb-6">
            <h2 className="text-lg font-semibold text-navy-900">
              Giriş Yap
            </h2>
            <p className="text-xs text-neutral-500 mt-0.5">
              Devam etmek için kurumsal hesabınızla oturum açın
            </p>
          </div>

          {/* Error Feedback Alerts */}
          {errorMessage && (
            <div
              role="alert"
              className={cn(
                'flex items-start gap-3 p-3.5 rounded-xl border text-xs leading-relaxed mb-5',
                errorType === 'rate-limit'
                  ? 'bg-warning-light border-amber-200 text-warning-dark'
                  : errorType === 'network'
                  ? 'bg-neutral-50 border-neutral-200 text-neutral-800'
                  : 'bg-danger-light border-red-200 text-danger-dark'
              )}
            >
              {errorType === 'rate-limit' ? (
                <AlertTriangle className="h-4 w-4 text-warning flex-shrink-0 mt-0.5" />
              ) : errorType === 'network' ? (
                <WifiOff className="h-4 w-4 text-neutral-600 flex-shrink-0 mt-0.5" />
              ) : (
                <AlertCircle className="h-4 w-4 text-danger flex-shrink-0 mt-0.5" />
              )}
              <div className="flex-1">
                <span className="font-semibold block mb-0.5">
                  {errorType === 'rate-limit'
                    ? 'İstek Limiti Aşıldı'
                    : errorType === 'network'
                    ? 'Bağlantı Hatası'
                    : 'Giriş Başarısız'}
                </span>
                <span>{errorMessage}</span>
              </div>
            </div>
          )}

          <form onSubmit={handleSubmit} noValidate className="space-y-4">
            {/* Email Field */}
            <div>
              <label
                htmlFor="login-email"
                className="block text-xs font-semibold text-neutral-700 mb-1.5"
              >
                E-posta
              </label>
              <div className="relative">
                <Mail className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
                <input
                  id="login-email"
                  type="email"
                  required
                  autoComplete="email"
                  disabled={isLoading}
                  value={email}
                  onChange={(e) => {
                    setEmail(e.target.value);
                    if (errorMessage) setErrorMessage(null);
                  }}
                  placeholder="ad.soyad@kurum.k12.tr"
                  className={cn(
                    'w-full pl-10 pr-4 py-2.5 rounded-xl border text-sm',
                    'bg-white text-neutral-900 placeholder:text-neutral-400',
                    'transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
                    errorMessage && errorType === 'auth'
                      ? 'border-danger ring-1 ring-danger'
                      : 'border-neutral-300 hover:border-neutral-400'
                  )}
                />
              </div>
            </div>

            {/* Password Field */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label
                  htmlFor="login-password"
                  className="block text-xs font-semibold text-neutral-700"
                >
                  Şifre
                </label>
                <Link
                  to="/forgot-password"
                  className="text-xs text-primary-600 hover:text-primary-700 font-medium transition-colors"
                >
                  Şifremi Unuttum
                </Link>
              </div>
              <div className="relative">
                <Lock className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
                <input
                  id="login-password"
                  type={showPassword ? 'text' : 'password'}
                  required
                  autoComplete="current-password"
                  disabled={isLoading}
                  value={password}
                  onChange={(e) => {
                    setPassword(e.target.value);
                    if (errorMessage) setErrorMessage(null);
                  }}
                  placeholder="••••••••"
                  className={cn(
                    'w-full pl-10 pr-11 py-2.5 rounded-xl border text-sm',
                    'bg-white text-neutral-900 placeholder:text-neutral-400',
                    'transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-transparent',
                    errorMessage && errorType === 'auth'
                      ? 'border-danger ring-1 ring-danger'
                      : 'border-neutral-300 hover:border-neutral-400'
                  )}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-700 transition-colors p-1 rounded-md focus:outline-none focus:ring-1 focus:ring-primary-500"
                  aria-label={showPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>

            {/* Submit Button */}
            <div className="pt-2">
              <button
                type="submit"
                disabled={isLoading}
                className={cn(
                  'w-full py-2.5 px-4 rounded-xl font-semibold text-sm text-white',
                  'bg-primary-600 hover:bg-primary-700 active:bg-primary-800',
                  'transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2',
                  'min-h-[44px] flex items-center justify-center gap-2 cursor-pointer shadow-soft-xs',
                  isLoading && 'opacity-70 cursor-wait'
                )}
              >
                {isLoading ? (
                  <>
                    <svg className="animate-spin h-4 w-4 text-white" viewBox="0 0 24 24" fill="none">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                    <span>Doğrulanıyor...</span>
                  </>
                ) : (
                  <span>Giriş Yap</span>
                )}
              </button>
            </div>
          </form>
        </div>

        {/* Institutional Footer Note */}
        <p className="text-center text-xs text-neutral-400 mt-6">
          Bilim Akademi Platform Güvenliği
        </p>
      </div>
    </div>
  );
};


