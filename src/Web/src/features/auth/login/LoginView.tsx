import React, { useState } from 'react';
import { useNavigate, useLocation, Link } from 'react-router-dom';
import { Mail, Lock, Eye, EyeOff, Layers, AlertTriangle, XCircle, ChevronDown } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/auth/useAuth';
import { UserRole } from '../types';
import { getRoleDefaultRoute } from '@/auth/roleRoutes';

const ROLE_LABELS: Record<UserRole, string> = {
  student: 'Öğrenci',
  coach: 'Koç',
  teacher: 'Öğretmen',
  parent: 'Veli',
  admin: 'Kurum Yöneticisi',
};

const DEMO_ROLES: UserRole[] = ['student', 'coach', 'teacher', 'parent', 'admin'];

export const LoginView: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, loginAsRole, isLoading } = useAuth();

  const [emailOrUsername, setEmailOrUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [errorStatus, setErrorStatus] = useState<'none' | 'invalid' | 'locked'>('none');
  const [demoModeOpen, setDemoModeOpen] = useState(false);

  const fromLocation = (location.state as { from?: { pathname: string } })?.from?.pathname;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isLoading) return;

    if (emailOrUsername === 'locked@example.com') {
      setErrorStatus('locked');
      return;
    }

    if (password === 'wrong' || (emailOrUsername && password.length < 4)) {
      setErrorStatus('invalid');
      return;
    }

    try {
      await login({ emailOrUsername, password });
      if (fromLocation) {
        navigate(fromLocation, { replace: true });
      } else {
        navigate('/student/today', { replace: true });
      }
    } catch {
      setErrorStatus('invalid');
    }
  };

  const handleDemoRoleSelect = (role: UserRole) => {
    if (!import.meta.env.DEV) return;
    loginAsRole(role);
    const targetRoute = getRoleDefaultRoute(role);
    navigate(targetRoute, { replace: true });
  };

  return (
    <div className="min-h-screen flex bg-surface select-none">
      {/* 1. Left Panel — Brand Hero (Desktop only) */}
      <div className="hidden lg:flex lg:w-1/2 xl:w-[55%] bg-navy-900 flex-col justify-between p-12 relative overflow-hidden">
        <div className="absolute inset-0 opacity-10 pointer-events-none">
          <div className="absolute top-1/4 left-1/4 w-64 h-64 rounded-full bg-primary-500 blur-3xl" />
          <div className="absolute bottom-1/3 right-1/4 w-80 h-80 rounded-full bg-navy-500 blur-3xl" />
        </div>

        {/* Brand Header */}
        <div className="relative z-10 flex items-center gap-3">
          <div className="w-10 h-10 rounded-xl bg-primary-600 flex items-center justify-center shadow-soft-xs">
            <Layers className="h-5 w-5 text-white" />
          </div>
          <div>
            <p className="text-white font-bold text-lg leading-tight">Bilim Akademi</p>
            <p className="text-navy-300 text-xs">Eğitim Platformu</p>
          </div>
        </div>

        {/* Hero Slogan */}
        <div className="relative z-10 space-y-4">
          <h1 className="font-serif text-white text-4xl leading-snug">
            Hedefine giden
            <br />
            en net yol.
          </h1>
          <p className="text-navy-200 text-base leading-relaxed max-w-sm">
            Öğrenci, koç, öğretmen ve kurum için tasarlanmış bütünleşik eğitim yönetim platformu.
          </p>
        </div>

        {/* Key Platform Stats */}
        <div className="relative z-10 flex gap-8">
          {[
            { num: '12.400+', label: 'Aktif Öğrenci' },
            { num: '480+', label: 'Uzman Koç' },
            { num: '%94', label: 'Hedef Başarı' },
          ].map((stat) => (
            <div key={stat.label}>
              <p className="text-white font-mono text-2xl font-bold">{stat.num}</p>
              <p className="text-navy-300 text-xs mt-0.5">{stat.label}</p>
            </div>
          ))}
        </div>
      </div>

      {/* 2. Right Panel — Form */}
      <div className="flex-1 flex flex-col justify-center px-6 py-12 sm:px-12 bg-surface">
        <div className="w-full max-w-sm mx-auto">
          {/* Mobile brand header */}
          <div className="lg:hidden flex items-center gap-3 mb-8">
            <div className="w-9 h-9 rounded-xl bg-primary-600 flex items-center justify-center">
              <Layers className="h-5 w-5 text-white" />
            </div>
            <div>
              <span className="font-bold text-navy-900 text-lg leading-tight block">
                Bilim Akademi
              </span>
              <span className="text-xs text-neutral-400">Eğitim Platformu</span>
            </div>
          </div>

          <h2 className="font-serif text-2xl sm:text-3xl text-neutral-900 mb-1 leading-tight">
            Tekrar hoş geldiniz
          </h2>
          <p className="text-neutral-500 text-sm mb-6 sm:mb-8">Hesabınıza giriş yapın</p>

          {/* Invalid Error Alert */}
          {errorStatus === 'invalid' && (
            <div className="flex items-start gap-3 p-4 rounded-xl bg-danger-light border border-red-200 mb-6">
              <XCircle className="h-4 w-4 text-danger flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-danger font-semibold text-sm">Giriş başarısız</p>
                <p className="text-red-700 text-xs mt-0.5">
                  E-posta veya şifre hatalı. Lütfen tekrar deneyin.
                </p>
              </div>
            </div>
          )}

          {/* Locked Account Alert */}
          {errorStatus === 'locked' && (
            <div className="flex items-start gap-3 p-4 rounded-xl bg-warning-light border border-amber-200 mb-6">
              <AlertTriangle className="h-4 w-4 text-warning flex-shrink-0 mt-0.5" />
              <div>
                <p className="text-warning-dark font-semibold text-sm">Hesap kilitlendi</p>
                <p className="text-amber-800 text-xs mt-0.5">
                  Çok fazla hatalı deneme yapıldı. Şifrenizi sıfırlayabilirsiniz.
                </p>
                <Link
                  to="/forgot-password"
                  className="text-xs text-warning-dark font-bold mt-2 inline-block underline underline-offset-2"
                >
                  Şifremi sıfırla
                </Link>
              </div>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Email / Username */}
            <div>
              <label htmlFor="email" className="block text-xs font-semibold text-neutral-700 mb-1.5">
                E-posta veya kullanıcı adı
              </label>
              <div className="relative">
                <Mail className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
                <input
                  id="email"
                  type="text"
                  required
                  autoComplete="username"
                  value={emailOrUsername}
                  onChange={(e) => {
                    setEmailOrUsername(e.target.value);
                    if (errorStatus !== 'none') setErrorStatus('none');
                  }}
                  placeholder="ornek@bilimakademi.k12.tr"
                  className={cn(
                    'w-full pl-10 pr-4 py-3 rounded-xl border text-sm',
                    'bg-white text-neutral-900 placeholder:text-neutral-400',
                    'transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs',
                    errorStatus === 'invalid'
                      ? 'border-danger ring-1 ring-danger'
                      : 'border-neutral-300 hover:border-neutral-400'
                  )}
                />
              </div>
            </div>

            {/* Password */}
            <div>
              <div className="flex items-center justify-between mb-1.5">
                <label htmlFor="password" className="block text-xs font-semibold text-neutral-700">
                  Şifre
                </label>
                <Link
                  to="/forgot-password"
                  className="text-xs text-primary-600 hover:text-primary-700 font-medium transition-colors"
                >
                  Şifremi unuttum
                </Link>
              </div>
              <div className="relative">
                <Lock className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  required
                  autoComplete="current-password"
                  value={password}
                  onChange={(e) => {
                    setPassword(e.target.value);
                    if (errorStatus !== 'none') setErrorStatus('none');
                  }}
                  placeholder="••••••••"
                  className={cn(
                    'w-full pl-10 pr-12 py-3 rounded-xl border text-sm',
                    'bg-white text-neutral-900 placeholder:text-neutral-400',
                    'transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs',
                    errorStatus === 'invalid'
                      ? 'border-danger ring-1 ring-danger'
                      : 'border-neutral-300 hover:border-neutral-400'
                  )}
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(!showPassword)}
                  className="absolute right-3.5 top-1/2 -translate-y-1/2 text-neutral-400 hover:text-neutral-700 transition-colors p-1"
                  aria-label={showPassword ? 'Şifreyi gizle' : 'Şifreyi göster'}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </button>
              </div>
            </div>

            {/* Submit Button */}
            <button
              type="submit"
              disabled={isLoading}
              className={cn(
                'w-full py-3 px-6 rounded-xl font-semibold text-sm text-white',
                'bg-primary-600 hover:bg-primary-700 active:bg-primary-800',
                'transition-all duration-150 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2',
                'min-h-[48px] flex items-center justify-center gap-2 shadow-soft-xs cursor-pointer',
                isLoading && 'opacity-80 cursor-wait'
              )}
            >
              {isLoading ? (
                <>
                  <svg className="animate-spin h-4 w-4 text-white" viewBox="0 0 24 24" fill="none">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                  </svg>
                  Giriş yapılıyor...
                </>
              ) : (
                'Giriş Yap'
              )}
            </button>
          </form>

          {/* 3. Development Role Preview Switcher — STRICTLY DEV ONLY */}
          {import.meta.env.DEV && (
            <div className="mt-8 pt-6 border-t border-neutral-200">
              <button
                type="button"
                onClick={() => setDemoModeOpen(!demoModeOpen)}
                className="w-full flex items-center justify-center gap-1.5 text-xs font-semibold text-neutral-500 hover:text-neutral-800 transition-colors py-1"
              >
                <span>{demoModeOpen ? 'Geliştirme Rol Seçimini Gizle' : 'Geliştirme Rol Seçimi (Önizleme)'}</span>
                <ChevronDown className={cn('h-3.5 w-3.5 transition-transform', demoModeOpen && 'rotate-180')} />
              </button>

              {demoModeOpen && (
                <div className="mt-4 p-3 bg-neutral-50 rounded-xl border border-neutral-200 space-y-2.5">
                  <p className="text-[11px] text-neutral-500 text-center font-medium">
                    Doğrudan test etmek istediğiniz rol portalını seçin:
                  </p>
                  <div className="grid grid-cols-2 sm:grid-cols-3 gap-2">
                    {DEMO_ROLES.map((r) => (
                      <button
                        key={r}
                        type="button"
                        onClick={() => handleDemoRoleSelect(r)}
                        className={cn(
                          'py-2 px-2.5 rounded-lg border text-xs font-semibold transition-all',
                          'bg-white border-neutral-200 text-neutral-700 hover:bg-primary-50 hover:border-primary-300 hover:text-primary-700 shadow-soft-2xs'
                        )}
                      >
                        {ROLE_LABELS[r]}
                      </button>
                    ))}
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
