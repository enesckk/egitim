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
  const { login, user, isLoading } = useAuth();

  const [emailOrUsername, setEmailOrUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [errorType, setErrorType] = useState<'auth' | 'rate-limit' | 'network' | 'validation' | null>(null);

  const fromLocation = (location.state as { from?: { pathname: string } })?.from?.pathname;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isLoading) return;

    const trimmedEmail = emailOrUsername.trim();
    if (!trimmedEmail || !password) {
      setErrorType('validation');
      setErrorMessage('Lütfen e-posta adresinizi ve şifrenizi giriniz.');
      return;
    }

    setErrorType(null);
    setErrorMessage(null);

    try {
      await login({ emailOrUsername: trimmedEmail, password });
      
      // Determine target destination
      if (fromLocation && fromLocation !== '/login' && fromLocation !== '/403') {
        navigate(fromLocation, { replace: true });
      } else if (user?.role) {
        navigate(getRoleDefaultRoute(user.role), { replace: true });
      } else {
        navigate('/student/today', { replace: true });
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
      } else {
        setErrorType('auth');
        setErrorMessage('Giriş yapılırken beklenmeyen bir hata oluştu. Lütfen tekrar deneyiniz.');
      }
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-[#F7F9FC] px-4 py-8 sm:px-6 lg:px-8 select-none">
      <div className="w-full max-w-md">
        {/* Institutional Brand Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-12 h-12 rounded-2xl bg-[#17324D] text-white shadow-sm mb-4">
            <Layers className="h-6 w-6 text-[#2A7F7B]" />
          </div>
          <h1 className="text-2xl font-bold tracking-tight text-[#17212B] font-sans">
            Bilim Akademi
          </h1>
          <p className="text-sm text-[#66788A] mt-1">
            Eğitim Yönetim ve Takip Platformu
          </p>
        </div>

        {/* Login Card */}
        <div className="bg-white rounded-2xl border border-[#D9E1E8] shadow-sm p-6 sm:p-8">
          <div className="mb-6">
            <h2 className="text-lg font-semibold text-[#17212B]">
              Giriş Yap
            </h2>
            <p className="text-xs text-[#66788A] mt-0.5">
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
                  ? 'bg-amber-50 border-amber-200 text-amber-900'
                  : errorType === 'network'
                  ? 'bg-slate-50 border-slate-200 text-slate-800'
                  : 'bg-red-50 border-red-200 text-red-900'
              )}
            >
              {errorType === 'rate-limit' ? (
                <AlertTriangle className="h-4 w-4 text-amber-600 flex-shrink-0 mt-0.5" />
              ) : errorType === 'network' ? (
                <WifiOff className="h-4 w-4 text-slate-600 flex-shrink-0 mt-0.5" />
              ) : (
                <AlertCircle className="h-4 w-4 text-red-600 flex-shrink-0 mt-0.5" />
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
            {/* Email / Username Field */}
            <div>
              <label
                htmlFor="login-email"
                className="block text-xs font-semibold text-[#17212B] mb-1.5"
              >
                E-posta veya Kullanıcı Adı
              </label>
              <div className="relative">
                <Mail className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-[#66788A] pointer-events-none" />
                <input
                  id="login-email"
                  type="text"
                  required
                  autoComplete="username"
                  disabled={isLoading}
                  value={emailOrUsername}
                  onChange={(e) => {
                    setEmailOrUsername(e.target.value);
                    if (errorMessage) setErrorMessage(null);
                  }}
                  placeholder="ad.soyad@kurum.k12.tr"
                  className={cn(
                    'w-full pl-10 pr-4 py-2.5 rounded-xl border text-sm',
                    'bg-white text-[#17212B] placeholder:text-[#66788A]/60',
                    'transition-colors focus:outline-none focus:ring-2 focus:ring-[#17324D] focus:border-transparent',
                    errorMessage && errorType === 'auth'
                      ? 'border-red-400 ring-1 ring-red-400'
                      : 'border-[#D9E1E8] hover:border-[#66788A]'
                  )}
                />
              </div>
            </div>

            {/* Password Field */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label
                  htmlFor="login-password"
                  className="block text-xs font-semibold text-[#17212B]"
                >
                  Şifre
                </label>
                <Link
                  to="/forgot-password"
                  className="text-xs text-[#2A7F7B] hover:text-[#17324D] font-medium transition-colors"
                >
                  Şifremi Unuttum
                </Link>
              </div>
              <div className="relative">
                <Lock className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-[#66788A] pointer-events-none" />
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
                    'bg-white text-[#17212B] placeholder:text-[#66788A]/60',
                    'transition-colors focus:outline-none focus:ring-2 focus:ring-[#17324D] focus:border-transparent',
                    errorMessage && errorType === 'auth'
                      ? 'border-red-400 ring-1 ring-red-400'
                      : 'border-[#D9E1E8] hover:border-[#66788A]'
                  )}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3 top-1/2 -translate-y-1/2 text-[#66788A] hover:text-[#17212B] transition-colors p-1 rounded-md focus:outline-none focus:ring-1 focus:ring-[#17324D]"
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
                  'bg-[#17324D] hover:bg-[#1f4060] active:bg-[#12263a]',
                  'transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-[#17324D] focus:ring-offset-2',
                  'min-h-[44px] flex items-center justify-center gap-2 cursor-pointer shadow-sm',
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
        <p className="text-center text-xs text-[#66788A] mt-6">
          Bilim Akademi Platform Güvenliği • 256-bit SSL Korumalı
        </p>
      </div>
    </div>
  );
};

