import React, { useState, useEffect } from 'react';
import { Bell, BookOpen, FileCheck, MessageSquare, Calendar, Folder, CheckCircle2, AlertCircle } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { notificationService, NotificationPreferencesDto } from '@/services/notificationService';

export interface NotificationSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const NotificationSettingsModal: React.FC<NotificationSettingsModalProps> = ({
  isOpen,
  onClose,
}) => {
  const [preferences, setPreferences] = useState<NotificationPreferencesDto>({
    inAppEnabled: true,
    examResultsEnabled: true,
    coachTasksEnabled: true,
    meetingsEnabled: true,
    messagesEnabled: true,
    materialsEnabled: true,
  });
  const [isLoading, setIsLoading] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [savedSuccess, setSavedSuccess] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setIsLoading(true);
      setErrorMessage(null);
      setSavedSuccess(false);
      notificationService.getPreferences()
        .then((data) => {
          if (data) setPreferences(data);
        })
        .catch(() => {
          setErrorMessage('Bildirim tercihleri sunucudan alınamadı.');
        })
        .finally(() => {
          setIsLoading(false);
        });
    }
  }, [isOpen]);

  const handleToggle = (key: keyof NotificationPreferencesDto) => {
    setPreferences((prev) => ({
      ...prev,
      [key]: !prev[key],
    }));
    setSavedSuccess(false);
  };

  const handleSave = async () => {
    setIsSaving(true);
    setErrorMessage(null);
    try {
      const updated = await notificationService.updatePreferences(preferences);
      setPreferences(updated);
      setSavedSuccess(true);
      setTimeout(() => setSavedSuccess(false), 3000);
    } catch {
      setErrorMessage('Tercihler kaydedilirken bir hata oluştu. Lütfen tekrar deneyin.');
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Bildirim Tercihleri"
      subtitle="Kişisel Bildirim Yapılandırması"
      headerVariant="dark"
      footer={
        <div className="flex justify-between items-center w-full">
          <Button
            variant="primary"
            size="sm"
            onClick={handleSave}
            disabled={isSaving || isLoading}
            id="btn-save-preferences"
          >
            {isSaving ? 'Kaydediliyor...' : 'Tercihleri Kaydet'}
          </Button>
          <Button variant="secondary" size="sm" onClick={onClose}>
            Kapat
          </Button>
        </div>
      }
    >
      <div className="space-y-4 select-none">
        {savedSuccess && (
          <div className="p-3 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs font-medium flex items-center gap-2" role="status">
            <CheckCircle2 className="h-4 w-4 text-emerald-600 flex-shrink-0" />
            <span>Bildirim tercihleriniz başarıyla veritabanına kaydedildi.</span>
          </div>
        )}

        {errorMessage && (
          <div className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-800 text-xs font-medium flex items-center gap-2" role="alert">
            <AlertCircle className="h-4 w-4 text-red-600 flex-shrink-0" />
            <span>{errorMessage}</span>
          </div>
        )}

        <div className="space-y-2.5 pt-1">
          <p className="text-xs font-semibold text-[#14233E] uppercase tracking-wide">
            Kayıtlı Bildirim Kanalları
          </p>

          <div className="space-y-2.5">
            <div className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/60 bg-white hover:bg-neutral-50/70 transition-colors min-h-[52px]">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-blue-50 text-[#315FE8]">
                  <Bell className="h-4 w-4" />
                </div>
                <div>
                  <label htmlFor="pref-in-app" className="text-xs font-bold text-[#14233E] cursor-pointer">Uygulama İçi Bildirimler</label>
                  <p className="text-[11px] text-neutral-500">Genel sistem ve panel bildirim rozeti</p>
                </div>
              </div>
              <input
                id="pref-in-app"
                type="checkbox"
                data-testid="pref-in-app"
                checked={preferences.inAppEnabled}
                onChange={() => handleToggle('inAppEnabled')}
                className="w-4 h-4 text-[#315FE8] rounded border-neutral-300 focus:ring-[#315FE8] cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/60 bg-white hover:bg-neutral-50/70 transition-colors min-h-[52px]">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-emerald-50 text-emerald-600">
                  <FileCheck className="h-4 w-4" />
                </div>
                <div>
                  <label htmlFor="pref-exam-results" className="text-xs font-bold text-[#14233E] cursor-pointer">Deneme Sınavı Bildirimleri</label>
                  <p className="text-[11px] text-neutral-500">Sınav analizleriniz ve sonuçlarınız yayımlandığında</p>
                </div>
              </div>
              <input
                id="pref-exam-results"
                type="checkbox"
                data-testid="pref-exam-results"
                checked={preferences.examResultsEnabled}
                onChange={() => handleToggle('examResultsEnabled')}
                className="w-4 h-4 text-[#315FE8] rounded border-neutral-300 focus:ring-[#315FE8] cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/60 bg-white hover:bg-neutral-50/70 transition-colors min-h-[52px]">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-indigo-50 text-indigo-600">
                  <BookOpen className="h-4 w-4" />
                </div>
                <div>
                  <label htmlFor="pref-coach-tasks" className="text-xs font-bold text-[#14233E] cursor-pointer">Koçluk Görevleri</label>
                  <p className="text-[11px] text-neutral-500">Yeni ödev ve görev tanımlandığında</p>
                </div>
              </div>
              <input
                id="pref-coach-tasks"
                type="checkbox"
                data-testid="pref-coach-tasks"
                checked={preferences.coachTasksEnabled}
                onChange={() => handleToggle('coachTasksEnabled')}
                className="w-4 h-4 text-[#315FE8] rounded border-neutral-300 focus:ring-[#315FE8] cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/60 bg-white hover:bg-neutral-50/70 transition-colors min-h-[52px]">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-amber-50 text-amber-600">
                  <Calendar className="h-4 w-4" />
                </div>
                <div>
                  <label htmlFor="pref-meetings" className="text-xs font-bold text-[#14233E] cursor-pointer">Görüşme Randevuları</label>
                  <p className="text-[11px] text-neutral-500">Koçluk görüşmesi planlandığında veya güncellendiğinde</p>
                </div>
              </div>
              <input
                id="pref-meetings"
                type="checkbox"
                data-testid="pref-meetings"
                checked={preferences.meetingsEnabled}
                onChange={() => handleToggle('meetingsEnabled')}
                className="w-4 h-4 text-[#315FE8] rounded border-neutral-300 focus:ring-[#315FE8] cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/60 bg-white hover:bg-neutral-50/70 transition-colors min-h-[52px]">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-blue-50 text-[#315FE8]">
                  <MessageSquare className="h-4 w-4" />
                </div>
                <div>
                  <label htmlFor="pref-messages" className="text-xs font-bold text-[#14233E] cursor-pointer">Mesajlaşma Bildirimleri</label>
                  <p className="text-[11px] text-neutral-500">Öğretmen veya koçunuzdan yeni mesaj geldiğinde</p>
                </div>
              </div>
              <input
                id="pref-messages"
                type="checkbox"
                data-testid="pref-messages"
                checked={preferences.messagesEnabled}
                onChange={() => handleToggle('messagesEnabled')}
                className="w-4 h-4 text-[#315FE8] rounded border-neutral-300 focus:ring-[#315FE8] cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/60 bg-white hover:bg-neutral-50/70 transition-colors min-h-[52px]">
              <div className="flex items-center gap-3">
                <div className="p-2 rounded-lg bg-teal-50 text-teal-600">
                  <Folder className="h-4 w-4" />
                </div>
                <div>
                  <label htmlFor="pref-materials" className="text-xs font-bold text-[#14233E] cursor-pointer">Ders Materyali Paylaşımları</label>
                  <p className="text-[11px] text-neutral-500">Sınıfınıza yeni ders materyali yüklendiğinde</p>
                </div>
              </div>
              <input
                id="pref-materials"
                type="checkbox"
                data-testid="pref-materials"
                checked={preferences.materialsEnabled}
                onChange={() => handleToggle('materialsEnabled')}
                className="w-4 h-4 text-[#315FE8] rounded border-neutral-300 focus:ring-[#315FE8] cursor-pointer"
              />
            </div>
          </div>
        </div>
      </div>
    </Modal>
  );
};
