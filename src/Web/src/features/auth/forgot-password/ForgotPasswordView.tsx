import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { Mail, Layers, CheckCircle2, ArrowLeft } from 'lucide-react';
import { useAuth } from '@/auth/useAuth';

export const ForgotPasswordView: React.FC = () => {
  const { requestPasswordReset, isLoading } = useAuth();
  const [email, setEmail] = useState('');
  const [isSubmitted, setIsSubmitted] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (isLoading) return;

    await requestPasswordReset(email);
    setIsSubmitted(true);
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

        {isSubmitted ? (
          <div className="text-center space-y-4">
            <div className="w-14 h-14 rounded-2xl bg-success-light flex items-center justify-center mx-auto shadow-soft-xs">
              <CheckCircle2 className="h-7 w-7 text-success" />
            </div>
            <h2 className="font-serif text-2xl text-neutral-900 leading-tight">
              Talep Alındı
            </h2>
            <p className="text-neutral-600 text-sm leading-relaxed">
              Eğer sistemimizde <strong className="text-neutral-900">{email}</strong> ile eşleşen aktif bir hesap varsa, şifre sıfırlama bağlantısı gönderilecektir.
            </p>
            <p className="text-xs text-neutral-400">
              Lütfen spam/gereksiz e-posta klasörünüzü de kontrol edin.
            </p>
            <div className="pt-2">
              <Link
                to="/login"
                className="w-full inline-flex items-center justify-center py-3 rounded-xl bg-primary-600 text-white text-sm font-semibold hover:bg-primary-700 transition-colors shadow-soft-xs"
              >
                Giriş Ekranına Dön
              </Link>
            </div>
          </div>
        ) : (
          <div className="space-y-6">
            <div>
              <h2 className="font-serif text-2xl sm:text-3xl text-neutral-900 mb-1 leading-tight">
                Şifremi Unuttum
              </h2>
              <p className="text-neutral-500 text-sm">
                Hesabınıza bağlı e-posta adresinizi girin.
              </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label htmlFor="reset-email" className="block text-xs font-semibold text-neutral-700 mb-1.5">
                  E-posta adresi
                </label>
                <div className="relative">
                  <Mail className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
                  <input
                    id="reset-email"
                    type="email"
                    required
                    autoComplete="email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    placeholder="ornek@bilimakademi.k12.tr"
                    className="w-full pl-10 pr-4 py-3 rounded-xl border border-neutral-300 hover:border-neutral-400 text-sm bg-white text-neutral-900 placeholder:text-neutral-400 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
                  />
                </div>
              </div>

              <button
                type="submit"
                disabled={isLoading}
                className="w-full py-3 px-6 rounded-xl font-semibold text-sm text-white bg-primary-600 hover:bg-primary-700 active:bg-primary-800 transition-colors min-h-[48px] flex items-center justify-center gap-2 shadow-soft-xs cursor-pointer disabled:opacity-80"
              >
                {isLoading ? (
                  <>
                    <svg className="animate-spin h-4 w-4 text-white" viewBox="0 0 24 24" fill="none">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
                    </svg>
                    Gönderiliyor...
                  </>
                ) : (
                  'Sıfırlama Bağlantısı Gönder'
                )}
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
