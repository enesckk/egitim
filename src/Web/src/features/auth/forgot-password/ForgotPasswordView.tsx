import React from 'react';
import { Link } from 'react-router-dom';
import { Layers, ArrowLeft, ShieldAlert } from 'lucide-react';

export const ForgotPasswordView: React.FC = () => {
  return (
    <div className="min-h-screen flex items-center justify-center bg-surface px-6 py-12 select-none">
      <div className="w-full max-w-md">
        {/* Brand Header */}
        <div className="flex items-center gap-3 mb-8 justify-center">
          <div className="w-10 h-10 rounded-xl bg-navy-900 flex items-center justify-center shadow-soft-xs text-white">
            <Layers className="h-5 w-5 text-primary-400" />
          </div>
          <div>
            <span className="font-bold text-navy-900 text-lg leading-tight block">
              Bilim Akademi
            </span>
            <span className="text-xs text-neutral-400">Eğitim Platformu</span>
          </div>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200 shadow-soft-sm p-6 sm:p-8 text-center space-y-5">
          <div className="w-14 h-14 rounded-2xl bg-neutral-100 flex items-center justify-center mx-auto text-navy-700">
            <ShieldAlert className="h-7 w-7 text-navy-800" />
          </div>

          <div>
            <h1 className="font-serif text-2xl text-navy-900 leading-tight">
              Şifre Sıfırlama Talebi
            </h1>
            <p className="text-neutral-600 text-sm mt-3 leading-relaxed">
              Kurumsal güvenlik ilkeleri doğrultusunda self-servis şifre sıfırlama hizmeti şu an kapalıdır.
            </p>
            <p className="text-xs text-neutral-500 mt-2 leading-relaxed bg-neutral-50 p-3 rounded-xl border border-neutral-100">
              Şifrenizi yenilemek veya hesabınıza erişim sağlamak için lütfen <strong>kurum yöneticiniz</strong> veya <strong>sistem idaresi</strong> ile iletişime geçiniz.
            </p>
          </div>

          <div className="pt-3">
            <Link
              to="/login"
              className="w-full inline-flex items-center justify-center py-2.5 px-4 rounded-xl bg-primary-600 text-white text-sm font-semibold hover:bg-primary-700 transition-colors shadow-soft-xs"
            >
              <ArrowLeft className="h-4 w-4 mr-2" />
              Giriş Ekranına Dön
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

