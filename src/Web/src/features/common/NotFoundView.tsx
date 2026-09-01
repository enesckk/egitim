import React from 'react';
import { useNavigate } from 'react-router-dom';
import { FileQuestion, ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { useAuth } from '@/auth/useAuth';
import { getRoleDefaultRoute } from '@/auth/roleRoutes';

export const NotFoundView: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const handleReturnHome = () => {
    if (user?.role) {
      navigate(getRoleDefaultRoute(user.role), { replace: true });
    } else {
      navigate('/login', { replace: true });
    }
  };

  return (
    <div className="min-h-screen bg-surface flex items-center justify-center px-4 sm:px-6 py-12 select-none">
      <div className="w-full max-w-md bg-white rounded-3xl border border-neutral-200 p-6 sm:p-8 text-center shadow-soft-md space-y-5">
        <div className="w-16 h-16 rounded-2xl bg-neutral-100 flex items-center justify-center mx-auto shadow-soft-xs">
          <FileQuestion className="h-8 w-8 text-neutral-500" />
        </div>

        <div>
          <span className="font-mono text-xs font-bold text-neutral-400 uppercase tracking-wider block mb-1">
            Hata 404
          </span>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Sayfa Bulunamadı
          </h1>
          <p className="text-neutral-500 text-sm mt-2 leading-relaxed">
            Ulaşmaya çalıştığınız sayfa taşınmış, silinmiş veya mevcut olmayabilir.
          </p>
        </div>

        <div className="pt-2">
          <Button
            variant="primary"
            size="md"
            className="w-full"
            onClick={handleReturnHome}
            leftIcon={<ArrowLeft className="h-4 w-4" />}
          >
            Ana Sayfaya Dön
          </Button>
        </div>
      </div>
    </div>
  );
};
