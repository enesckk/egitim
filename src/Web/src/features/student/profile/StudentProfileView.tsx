import React, { useState } from 'react';
import { Bell, Lock, Shield, ChevronRight, LogOut, AlertCircle, RefreshCw, CheckCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { ProfileIdentityCard } from './components/ProfileIdentityCard';
import { PasswordChangeModal } from './components/PasswordChangeModal';
import { NotificationSettingsModal } from './components/NotificationSettingsModal';
import { LogoutConfirmModal } from './components/LogoutConfirmModal';
import { useAuth } from '@/auth';
import { StudentProfileData } from './types';
import { useNavigate } from 'react-router-dom';

export interface StudentProfileViewProps {
  initialData?: StudentProfileData;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
  onLogout?: () => void;
}

export const StudentProfileView: React.FC<StudentProfileViewProps> = ({
  initialData,
  isLoading = false,
  errorMessage,
  onRetry,
  onLogout,
}) => {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  // Authoritative student full name matching Header.tsx
  const authoritativeName =
    user?.name && !user.name.includes('@') && !user.name.includes('.demo')
      ? user.name
      : 'Ahmet Yılmaz';

  const defaultProfile: StudentProfileData = {
    id: user?.id || 'student-current',
    name: authoritativeName,
    subtitle: `${user?.institutionName || 'Demo Eğitim Kurumu'} • ${user?.roleLabel || 'Öğrenci'}`,
    email: user?.email || 'student.demo@egitim.com',
    initials: authoritativeName
      .split(' ')
      .filter(Boolean)
      .map((w) => w[0])
      .join('')
      .substring(0, 2)
      .toUpperCase() || 'AY',
  };

  const profile = initialData || defaultProfile;
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [isNotifModalOpen, setIsNotifModalOpen] = useState(false);
  const [isLogoutModalOpen, setIsLogoutModalOpen] = useState(false);
  const [toastMessage, setToastMessage] = useState<string | null>(null);

  const handleConfirmLogout = () => {
    setIsLogoutModalOpen(false);
    if (onLogout) {
      onLogout();
    } else {
      logout();
      navigate('/login', { replace: true });
    }
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto space-y-4 select-none px-4 sm:px-0">
        <Skeleton className="h-40 w-full rounded-3xl" />
        <Skeleton className="h-48 w-full rounded-2xl" />
        <Skeleton className="h-48 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-2xl mx-auto py-8 space-y-4 px-4 sm:px-0">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Profil Yüklenemedi">
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button variant="primary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto select-none space-y-4 sm:space-y-6 pb-20 sm:pb-12 px-4 sm:px-0">
      {/* Header matching premium student screens */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl sm:text-3xl font-bold tracking-tight text-[#14233E]" data-testid="profile-page-title">
            Profilim ve Hesap Ayarlarım
          </h1>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1">
            Öğrenci kimlik bilgileriniz, iletişim kanallarınız ve güvenlik yapılandırmanız
          </p>
        </div>
      </div>

      {toastMessage && (
        <div className="p-3.5 rounded-xl bg-blue-50 border border-blue-200 text-blue-900 text-xs flex items-center justify-between" role="status">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="h-4 w-4 text-[#315FE8] flex-shrink-0" />
            <span>{toastMessage}</span>
          </div>
          <button
            type="button"
            onClick={() => setToastMessage(null)}
            className="text-blue-700 hover:text-blue-900 font-semibold ml-2 text-xs"
          >
            Tamam
          </button>
        </div>
      )}

      {/* 1. Identity & Account Details */}
      <ProfileIdentityCard
        profile={profile}
        institutionName={user?.institutionName || 'Demo Eğitim Kurumu'}
        roleLabel={user?.roleLabel || 'Öğrenci'}
      />

      {/* 2. Responsive Settings & Security Card */}
      <div className="bg-white rounded-2xl border border-neutral-200/70 overflow-hidden shadow-xs">
        <div className="px-5 py-3.5 border-b border-neutral-100 bg-[#F8F7F3]/70">
          <p className="text-xs font-bold text-[#14233E] uppercase tracking-wider">
            Hesap Tercihleri ve Güvenlik
          </p>
        </div>

        <div className="divide-y divide-neutral-100">
          {/* Notification Preferences */}
          <button
            type="button"
            data-testid="btn-open-notification-settings"
            onClick={() => setIsNotifModalOpen(true)}
            className="flex items-center gap-3.5 px-5 py-4 w-full hover:bg-neutral-50/80 transition-colors group text-left min-h-[56px]"
          >
            <div className="p-2 rounded-lg bg-blue-50 text-[#315FE8] group-hover:bg-[#315FE8] group-hover:text-white transition-colors">
              <Bell className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-[#14233E] group-hover:text-[#315FE8] transition-colors">
                Bildirim Tercihleri
              </p>
              <p className="text-xs text-neutral-500">
                Sınav sonuçları, koçluk görevleri ve mesaj bildirimlerini özelleştirin
              </p>
            </div>
            <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-neutral-600 transition-colors" />
          </button>

          {/* Password & Security */}
          <button
            type="button"
            data-testid="btn-open-password-settings"
            onClick={() => setIsPasswordModalOpen(true)}
            className="flex items-center gap-3.5 px-5 py-4 w-full hover:bg-neutral-50/80 transition-colors group text-left min-h-[56px]"
          >
            <div className="p-2 rounded-lg bg-emerald-50 text-emerald-600 group-hover:bg-emerald-600 group-hover:text-white transition-colors">
              <Lock className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-[#14233E] group-hover:text-[#315FE8] transition-colors">
                Şifre ve Hesap Güvenliği
              </p>
              <p className="text-xs text-neutral-500">
                Öğrenci giriş şifrenizi yenileyin ve güvenlik durumunu görüntüleyin
              </p>
            </div>
            <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-neutral-600 transition-colors" />
          </button>

          {/* Kurumsal Yönetim Bilgisi */}
          <div className="flex items-center gap-3.5 px-5 py-4 bg-neutral-50/40">
            <div className="p-2 rounded-lg bg-neutral-100 text-neutral-500">
              <Shield className="h-4 w-4" />
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-xs font-semibold text-neutral-700">Kurumsal Yönetim Denetimi</p>
              <p className="text-[11px] text-neutral-500">
                Öğrenci kayıtlı şube, sınıf ve danışman koç atamaları okul idareniz tarafından denetlenir.
              </p>
            </div>
          </div>
        </div>
      </div>

      {/* 3. Restrained, Safe Logout Action */}
      <div className="pt-2 text-center">
        <button
          type="button"
          data-testid="btn-profile-logout"
          onClick={() => setIsLogoutModalOpen(true)}
          className="inline-flex items-center gap-2 text-xs sm:text-sm font-semibold text-neutral-500 hover:text-red-600 transition-colors py-2.5 px-5 rounded-xl hover:bg-red-50 min-h-[44px]"
        >
          <LogOut className="h-4 w-4" />
          <span>Güvenli Çıkış Yap</span>
        </button>
      </div>

      {/* Modals */}
      <PasswordChangeModal
        isOpen={isPasswordModalOpen}
        onClose={() => setIsPasswordModalOpen(false)}
      />

      <NotificationSettingsModal
        isOpen={isNotifModalOpen}
        onClose={() => setIsNotifModalOpen(false)}
      />

      <LogoutConfirmModal
        isOpen={isLogoutModalOpen}
        onClose={() => setIsLogoutModalOpen(false)}
        onConfirm={handleConfirmLogout}
      />
    </div>
  );
};
