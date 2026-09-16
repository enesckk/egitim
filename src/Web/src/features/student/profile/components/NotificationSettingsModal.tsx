import React from 'react';
import { Bell, Mail, BookOpen, FileCheck } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';

export interface NotificationSettingsModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const NotificationSettingsModal: React.FC<NotificationSettingsModalProps> = ({
  isOpen,
  onClose,
}) => {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Bildirim Tercihleri"
      subtitle="Kurumsal Bildirim Yapılandırması"
      headerVariant="dark"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button variant="secondary" size="sm" onClick={onClose}>
            Kapat
          </Button>
        </div>
      }
    >
      <div className="space-y-4 select-none">
        <Alert variant="info" icon={<Bell className="h-5 w-5" />} title="Merkezi Bildirim Yönetimi">
          Bildirim tercihleri henüz bu platform üzerinden değiştirilemiyor. Bildirim yapılandırması kurum yöneticiniz tarafından merkezi olarak yönetilmektedir.
        </Alert>

        <div className="space-y-2.5 pt-1">
          <p className="text-xs font-semibold text-neutral-500 uppercase tracking-wide">
            Kayıtlı Bildirim Kanalları
          </p>

          <div className="space-y-2">
            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <Mail className="h-4 w-4 text-neutral-400 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">E-posta Bildirimleri</p>
                  <p className="text-[11px] text-neutral-400">Haftalık özet raporları ve önemli duyurular</p>
                </div>
              </div>
              <span className="text-[11px] font-medium text-neutral-500 bg-neutral-100 px-2.5 py-1 rounded-full border border-neutral-200">
                Merkezi Yönetim
              </span>
            </div>

            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <BookOpen className="h-4 w-4 text-neutral-400 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Çalışma ve Plan Hatırlatıcıları</p>
                  <p className="text-[11px] text-neutral-400">Günün çalışma saatlerinde hatırlatma bildirimleri</p>
                </div>
              </div>
              <span className="text-[11px] font-medium text-neutral-500 bg-neutral-100 px-2.5 py-1 rounded-full border border-neutral-200">
                Merkezi Yönetim
              </span>
            </div>

            <div className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 bg-neutral-50/50 min-h-[52px]">
              <div className="flex items-center gap-3">
                <FileCheck className="h-4 w-4 text-neutral-400 flex-shrink-0" />
                <div>
                  <p className="text-xs font-semibold text-neutral-800">Deneme Sınavı Uyarıları</p>
                  <p className="text-[11px] text-neutral-400">Sınav analizleriniz ve kurum dereceniz hazır olduğunda</p>
                </div>
              </div>
              <span className="text-[11px] font-medium text-neutral-500 bg-neutral-100 px-2.5 py-1 rounded-full border border-neutral-200">
                Merkezi Yönetim
              </span>
            </div>
          </div>
        </div>
      </div>
    </Modal>
  );
};
