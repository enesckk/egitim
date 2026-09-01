import React from 'react';
import { useNavigate } from 'react-router-dom';
import { ShieldAlert, ArrowLeft, LogOut } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { useAuth } from '@/auth/useAuth';
import { getRoleDefaultRoute } from '@/auth/roleRoutes';

export const ForbiddenView: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const handleReturnHome = () => {
    if (user?.role) {
      navigate(getRoleDefaultRoute(user.role), { replace: true });
    } else {
      navigate('/login', { replace: true });
    }
  };

  const handleLogout = () => {
    logout();
    navigate('/login', { replace: true });
  };

  return (
    <div className="min-h-screen bg-surface flex items-center justify-center px-4 sm:px-6 py-12 select-none">
      <div className="w-full max-w-md bg-white rounded-3xl border border-neutral-200 p-6 sm:p-8 text-center shadow-soft-md space-y-5">
        <div className="w-16 h-16 rounded-2xl bg-danger-light flex items-center justify-center mx-auto shadow-soft-xs">
          <ShieldAlert className="h-8 w-8 text-danger" />
        </div>

        <div>
          <span className="font-mono text-xs font-bold text-danger uppercase tracking-wider block mb-1">
            Hata 403
          </span>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Yetkisiz Erişim
          </h1>
          <p className="text-neutral-500 text-sm mt-2 leading-relaxed">
            Bu sayfayı veya modülü görüntülemek için gerekli rol yetkisine sahip değilsiniz.
          </p>
          {user && (
            <p className="text-xs text-neutral-400 mt-2 bg-neutral-50 p-2.5 rounded-xl border border-neutral-100">
              Giriş yapılan hesap: <strong className="text-neutral-700">{user.name}</strong> ({user.roleLabel})
            </p>
          )}
        </div>

        <div className="flex flex-col sm:flex-row gap-3 pt-2">
          <Button
            variant="primary"
            size="md"
            className="flex-1"
            onClick={handleReturnHome}
            leftIcon={<ArrowLeft className="h-4 w-4" />}
          >
            Ana Sayfama Dön
          </Button>

          <Button
            variant="secondary"
            size="md"
            onClick={handleLogout}
            leftIcon={<LogOut className="h-4 w-4 text-neutral-500" />}
          >
            Çıkış Yap
          </Button>
        </div>
      </div>
    </div>
  );
};
