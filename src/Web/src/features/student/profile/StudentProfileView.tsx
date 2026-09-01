import React, { useState } from 'react';
import { Bell, Lock, Settings, ChevronRight, LogOut, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { ProfileIdentityCard } from './components/ProfileIdentityCard';
import { PasswordChangeModal } from './components/PasswordChangeModal';
import { NotificationSettingsModal } from './components/NotificationSettingsModal';
import { LogoutConfirmModal } from './components/LogoutConfirmModal';
import { initialStudentProfileData } from './mockData';
import { StudentProfileData } from './types';

export interface StudentProfileViewProps {
  initialData?: StudentProfileData;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
  onLogout?: () => void;
}

export const StudentProfileView: React.FC<StudentProfileViewProps> = ({
  initialData = initialStudentProfileData,
  isLoading = false,
  errorMessage,
  onRetry,
  onLogout,
}) => {
  const [profile, setProfile] = useState<StudentProfileData>(initialData);
  const [isPasswordModalOpen, setIsPasswordModalOpen] = useState(false);
  const [isNotifModalOpen, setIsNotifModalOpen] = useState(false);
  const [isLogoutModalOpen, setIsLogoutModalOpen] = useState(false);
  const [infoAlert, setInfoAlert] = useState<string | null>(null);

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-lg mx-auto space-y-4 select-none">
        <Skeleton className="h-28 w-full rounded-2xl" />
        <Skeleton className="h-44 w-full rounded-2xl" />
        <Skeleton className="h-44 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-lg mx-auto py-8 space-y-4">
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
    <div className="max-w-lg mx-auto select-none space-y-3.5 sm:space-y-4">
      {/* Page Title (Figma Profile.tsx) */}
      <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
        Profil
      </h1>

      {infoAlert && (
        <Alert variant="info" onClose={() => setInfoAlert(null)}>
          {infoAlert}
        </Alert>
      )}

      {/* 1. Identity & Account Card */}
      <ProfileIdentityCard profile={profile} />

      {/* 2. Settings Menu (Matching Figma Profile.tsx) */}
      <div className="bg-white rounded-2xl border border-neutral-100 overflow-hidden shadow-soft-sm">
        <div className="px-5 py-3 border-b border-neutral-100 bg-neutral-50">
          <p className="text-xs font-semibold text-neutral-500 uppercase tracking-wide">
            Ayarlar
          </p>
        </div>
        <div className="divide-y divide-neutral-50">
          <button
            type="button"
            onClick={() => setIsNotifModalOpen(true)}
            className="flex items-center gap-3 px-5 py-3.5 w-full hover:bg-neutral-50 transition-colors group text-left min-h-[52px]"
          >
            <Bell className="h-4 w-4 text-neutral-400 flex-shrink-0" />
            <span className="flex-1 text-sm text-neutral-700 font-medium">Bildirim Tercihleri</span>
            <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-neutral-500 transition-colors" />
          </button>

          <button
            type="button"
            onClick={() => setIsPasswordModalOpen(true)}
            className="flex items-center gap-3 px-5 py-3.5 w-full hover:bg-neutral-50 transition-colors group text-left min-h-[52px]"
          >
            <Lock className="h-4 w-4 text-neutral-400 flex-shrink-0" />
            <span className="flex-1 text-sm text-neutral-700 font-medium">Şifre Değiştir</span>
            <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-neutral-500 transition-colors" />
          </button>

          <button
            type="button"
            onClick={() => setInfoAlert('Hesap ve gizlilik ayarlarınız günceldir.')}
            className="flex items-center gap-3 px-5 py-3.5 w-full hover:bg-neutral-50 transition-colors group text-left min-h-[52px]"
          >
            <Settings className="h-4 w-4 text-neutral-400 flex-shrink-0" />
            <span className="flex-1 text-sm text-neutral-700 font-medium">Hesap Ayarları</span>
            <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-neutral-500 transition-colors" />
          </button>
        </div>
      </div>

      {/* 3. Logout Action (Matching Figma Profile.tsx: low hierarchy, restrained) */}
      <div className="text-center pt-2">
        <button
          type="button"
          onClick={() => setIsLogoutModalOpen(true)}
          className="inline-flex items-center gap-2 text-xs sm:text-sm text-neutral-400 hover:text-danger transition-colors py-2 px-4 rounded-xl hover:bg-danger-light min-h-[44px]"
        >
          <LogOut className="h-4 w-4" />
          Çıkış Yap
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
        initialPreferences={profile.notificationPreferences}
        onSave={(prefs) => {
          setProfile((prev) => ({ ...prev, notificationPreferences: prefs }));
          setInfoAlert('Bildirim tercihleriniz başarıyla kaydedildi.');
        }}
      />

      <LogoutConfirmModal
        isOpen={isLogoutModalOpen}
        onClose={() => setIsLogoutModalOpen(false)}
        onConfirm={() => {
          setIsLogoutModalOpen(false);
          if (onLogout) onLogout();
          else setInfoAlert('Oturum başarıyla kapatıldı.');
        }}
      />
    </div>
  );
};
