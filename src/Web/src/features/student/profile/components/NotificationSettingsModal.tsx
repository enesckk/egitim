import React, { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';

export interface NotificationSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
  initialPreferences: {
    emailNotifications: boolean;
    smsNotifications: boolean;
    studyReminders: boolean;
    examResultsAlerts: boolean;
  };
  onSave: (prefs: {
    emailNotifications: boolean;
    smsNotifications: boolean;
    studyReminders: boolean;
    examResultsAlerts: boolean;
  }) => void;
}

export const NotificationSettingsModal: React.FC<NotificationSettingsModalProps> = ({
  isOpen,
  onClose,
  initialPreferences,
  onSave,
}) => {
  const [prefs, setPrefs] = useState(initialPreferences);

  const toggle = (key: keyof typeof initialPreferences) => {
    setPrefs((prev) => ({ ...prev, [key]: !prev[key] }));
  };

  const handleSave = () => {
    onSave(prefs);
    onClose();
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Bildirim Tercihleri"
      subtitle="Hangi durumlarda bildirim almak istediğinizi yönetin"
      headerVariant="dark"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button variant="secondary" size="sm" onClick={onClose}>
            Vazgeç
          </Button>
          <Button variant="primary" size="sm" onClick={handleSave}>
            Kaydet
          </Button>
        </div>
      }
    >
      <div className="space-y-3 divide-y divide-neutral-100 select-none">
        <div className="flex items-center justify-between pt-2">
          <div>
            <p className="text-sm font-semibold text-neutral-800">E-posta Bildirimleri</p>
            <p className="text-xs text-neutral-400">Haftalık özet raporları ve önemli duyurular</p>
          </div>
          <input
            type="checkbox"
            checked={prefs.emailNotifications}
            onChange={() => toggle('emailNotifications')}
            className="w-5 h-5 rounded text-primary-600 focus:ring-primary-500 cursor-pointer"
          />
        </div>

        <div className="flex items-center justify-between pt-3">
          <div>
            <p className="text-sm font-semibold text-neutral-800">Çalışma ve Plan Hatırlatıcıları</p>
            <p className="text-xs text-neutral-400">Günün çalışma saatlerinde hatırlatma bildirimleri</p>
          </div>
          <input
            type="checkbox"
            checked={prefs.studyReminders}
            onChange={() => toggle('studyReminders')}
            className="w-5 h-5 rounded text-primary-600 focus:ring-primary-500 cursor-pointer"
          />
        </div>

        <div className="flex items-center justify-between pt-3">
          <div>
            <p className="text-sm font-semibold text-neutral-800">Deneme Sınavı Açıklanma Uyarıları</p>
            <p className="text-xs text-neutral-400">Deneme analiziniz ve kurum dereceniz hazır olduğunda</p>
          </div>
          <input
            type="checkbox"
            checked={prefs.examResultsAlerts}
            onChange={() => toggle('examResultsAlerts')}
            className="w-5 h-5 rounded text-primary-600 focus:ring-primary-500 cursor-pointer"
          />
        </div>
      </div>
    </Modal>
  );
};
