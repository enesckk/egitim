import React, { useState, useEffect } from 'react';
import { Bell, BookOpen, FileCheck, MessageSquare, Calendar, Folder } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
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
  const [isSaving, setIsSaving] = useState(false);
  const [savedSuccess, setSavedSuccess] = useState(false);

  useEffect(() => {
    if (isOpen) {
      notificationService.getPreferences()
        .then((data) => {
          if (data) setPreferences(data);
        })
        .catch(() => {});
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
    try {
      const updated = await notificationService.updatePreferences(preferences);
      setPreferences(updated);
      setSavedSuccess(true);
      setTimeout(() => setSavedSuccess(false), 3000);
    } catch {
      // non-blocking
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
            disabled={isSaving}
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
          <Alert variant="success">
            Bildirim tercihleriniz başarıyla güncellendi.
          </Alert>
        )}

        <div className="space-y-2.5 pt-1">
          <p className="text-xs font-semibold text-neutral-500 uppercase tracking-wide">
            Kayıtlı Bildirim Kanalları
          </p>

          <div className="space-y-2">
            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <Bell className="h-4 w-4 text-primary-600 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Uygulama İçi Bildirimler</p>
                  <p className="text-[11px] text-neutral-400">Genel sistem ve panel bildirim rozeti</p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={preferences.inAppEnabled}
                onChange={() => handleToggle('inAppEnabled')}
                className="w-4 h-4 text-primary-600 rounded border-neutral-300 focus:ring-primary-500 cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <FileCheck className="h-4 w-4 text-emerald-600 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Deneme Sınavı Bildirimleri</p>
                  <p className="text-[11px] text-neutral-400">Sınav analizleriniz ve sonuçlarınız yayımlandığında</p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={preferences.examResultsEnabled}
                onChange={() => handleToggle('examResultsEnabled')}
                className="w-4 h-4 text-primary-600 rounded border-neutral-300 focus:ring-primary-500 cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <BookOpen className="h-4 w-4 text-indigo-600 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Koçluk Görevleri</p>
                  <p className="text-[11px] text-neutral-400">Yeni ödev ve görev tanımlandığında</p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={preferences.coachTasksEnabled}
                onChange={() => handleToggle('coachTasksEnabled')}
                className="w-4 h-4 text-primary-600 rounded border-neutral-300 focus:ring-primary-500 cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <Calendar className="h-4 w-4 text-amber-600 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Görüşme Randevuları</p>
                  <p className="text-[11px] text-neutral-400">Koçluk görüşmesi planlandığında veya güncellendiğinde</p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={preferences.meetingsEnabled}
                onChange={() => handleToggle('meetingsEnabled')}
                className="w-4 h-4 text-primary-600 rounded border-neutral-300 focus:ring-primary-500 cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <MessageSquare className="h-4 w-4 text-blue-600 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Mesajlaşma Bildirimleri</p>
                  <p className="text-[11px] text-neutral-400">Öğretmen veya koçunuzdan yeni mesaj geldiğinde</p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={preferences.messagesEnabled}
                onChange={() => handleToggle('messagesEnabled')}
                className="w-4 h-4 text-primary-600 rounded border-neutral-300 focus:ring-primary-500 cursor-pointer"
              />
            </div>

            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <Folder className="h-4 w-4 text-teal-600 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Ders Materyali Paylaşımları</p>
                  <p className="text-[11px] text-neutral-400">Sınıfınıza yeni ders materyali yüklendiğinde</p>
                </div>
              </div>
              <input
                type="checkbox"
                checked={preferences.materialsEnabled}
                onChange={() => handleToggle('materialsEnabled')}
                className="w-4 h-4 text-primary-600 rounded border-neutral-300 focus:ring-primary-500 cursor-pointer"
              />
            </div>
          </div>
        </div>
      </div>
    </Modal>
  );
};
