import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Lock, Eye, EyeOff, Layers, CheckCircle2, ArrowLeft } from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/auth/useAuth';

export const ResetPasswordView: React.FC = () => {
  const { resetPassword, isLoading } = useAuth();
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isSuccess, setIsSuccess] = useState(false);
  const [errorMsg, setErrorMsg] = useState('');

  const hasMinLength = password.length >= 8;
  const hasUpperCase = /[A-Z]/.test(password);
  const hasNumber = /[0-9]/.test(password);
  const passwordsMatch = password.length > 0 && password === confirmPassword;

  const isFormValid = hasMinLength && hasUpperCase && hasNumber && passwordsMatch;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!isFormValid || isLoading) return;

    if (password !== confirmPassword) {
      setErrorMsg('Şifreler birbiriyle eşleşmiyor.');
      return;
    }

    await resetPassword({ password, confirmPassword });
    setIsSuccess(true);
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-surface px-6 py-12 select-none">
      <div className="w-full max-w-sm">
        {/* Brand Header */}
        <div className="flex items-center gap-3 mb-8">
          <div className="w-9 h-9 rounded-xl bg-primary-600 flex items-center justify-center shadow-soft-xs">
            <Layers className="h-5 w-5 text-white" />
          </div>
          <div>
            <span className="font-bold text-navy-900 text-lg leading-tight block">
              Bilim Akademi
            </span>
            <span className="text-xs text-neutral-400">Eğitim Platformu</span>
          </div>
        </div>

        {isSuccess ? (
          <div className="text-center space-y-4">
            <div className="w-14 h-14 rounded-2xl bg-success-light flex items-center justify-center mx-auto shadow-soft-xs">
              <CheckCircle2 className="h-7 w-7 text-success" />
            </div>
            <h2 className="font-serif text-2xl text-neutral-900 leading-tight">
              Şifreniz Güncellendi
            </h2>
            <p className="text-neutral-600 text-sm leading-relaxed">
              Yeni şifreniz başarıyla tanımlandı. Artık yeni şifrenizle giriş yapabilirsiniz.
            </p>
            <div className="pt-2">
              <Link
                to="/login"
                className="w-full inline-flex items-center justify-center py-3 rounded-xl bg-primary-600 text-white text-sm font-semibold hover:bg-primary-700 transition-colors shadow-soft-xs"
              >
                Giriş Yap
              </Link>
            </div>
          </div>
        ) : (
          <div className="space-y-6">
            <div>
              <h2 className="font-serif text-2xl sm:text-3xl text-neutral-900 mb-1 leading-tight">
                Yeni Şifre Belirleyin
              </h2>
              <p className="text-neutral-500 text-sm">
                Lütfen güvenli bir şifre belirleyin.
              </p>
            </div>

            {errorMsg && (
              <div className="p-3 bg-danger-light border border-red-200 text-danger text-xs rounded-xl font-medium">
                {errorMsg}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              {/* New Password */}
              <div>
                <label htmlFor="new-password" className="block text-xs font-semibold text-neutral-700 mb-1.5">
                  Yeni Şifre
                </label>
                <div className="relative">
                  <Lock className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
                  <input
                    id="new-password"
                    type={showPassword ? 'text' : 'password'}
                    required
                    autoComplete="new-password"
                    value={password}
                    onChange={(e) => {
                      setPassword(e.target.value);
                      if (errorMsg) setErrorMsg('');
                    }}
                    placeholder="••••••••"
                    className="w-full pl-10 pr-12 py-3 rounded-xl border border-neutral-300 hover:border-neutral-400 text-sm bg-white text-neutral-900 placeholder:text-neutral-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
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

              {/* Confirm Password */}
              <div>
                <label htmlFor="confirm-password" className="block text-xs font-semibold text-neutral-700 mb-1.5">
                  Şifreyi Tekrar Girin
                </label>
                <div className="relative">
                  <Lock className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
                  <input
                    id="confirm-password"
                    type={showPassword ? 'text' : 'password'}
                    required
                    autoComplete="new-password"
                    value={confirmPassword}
                    onChange={(e) => {
                      setConfirmPassword(e.target.value);
                      if (errorMsg) setErrorMsg('');
                    }}
                    placeholder="••••••••"
                    className="w-full pl-10 pr-4 py-3 rounded-xl border border-neutral-300 hover:border-neutral-400 text-sm bg-white text-neutral-900 placeholder:text-neutral-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
                  />
                </div>
              </div>

              {/* Validation Checklist */}
              <div className="bg-surface-alt p-3 rounded-xl border border-neutral-100 space-y-1.5 text-xs">
                <p className="font-semibold text-neutral-600 mb-1">Şifre Gereksinimleri:</p>
                <div className={cn('flex items-center gap-1.5', hasMinLength ? 'text-success font-semibold' : 'text-neutral-400')}>
                  <div className={cn('w-1.5 h-1.5 rounded-full', hasMinLength ? 'bg-success' : 'bg-neutral-300')} />
                  <span>En az 8 karakter</span>
                </div>
                <div className={cn('flex items-center gap-1.5', hasUpperCase ? 'text-success font-semibold' : 'text-neutral-400')}>
                  <div className={cn('w-1.5 h-1.5 rounded-full', hasUpperCase ? 'bg-success' : 'bg-neutral-300')} />
                  <span>En az 1 büyük harf</span>
                </div>
                <div className={cn('flex items-center gap-1.5', hasNumber ? 'text-success font-semibold' : 'text-neutral-400')}>
                  <div className={cn('w-1.5 h-1.5 rounded-full', hasNumber ? 'bg-success' : 'bg-neutral-300')} />
                  <span>En az 1 rakam</span>
                </div>
                {confirmPassword && (
                  <div className={cn('flex items-center gap-1.5', passwordsMatch ? 'text-success font-semibold' : 'text-danger font-semibold')}>
                    <div className={cn('w-1.5 h-1.5 rounded-full', passwordsMatch ? 'bg-success' : 'bg-danger')} />
                    <span>Şifreler eşleşiyor</span>
                  </div>
                )}
              </div>

              <button
                type="submit"
                disabled={!isFormValid || isLoading}
                className="w-full py-3 px-6 rounded-xl font-semibold text-sm text-white bg-primary-600 hover:bg-primary-700 active:bg-primary-800 transition-colors min-h-[48px] flex items-center justify-center gap-2 shadow-soft-xs cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed"
              >
                {isLoading ? 'Kaydediliyor...' : 'Şifreyi Güncelle'}
              </button>
            </form>

            <div className="text-center pt-2">
              <Link
                to="/login"
                className="inline-flex items-center gap-1.5 text-xs font-semibold text-neutral-500 hover:text-neutral-800 transition-colors py-1"
              >
                <ArrowLeft className="h-3.5 w-3.5" />
                Giriş ekranına dön
              </Link>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
