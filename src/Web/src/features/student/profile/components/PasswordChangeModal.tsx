import React from 'react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { ShieldAlert } from 'lucide-react';

export interface PasswordChangeModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const PasswordChangeModal: React.FC<PasswordChangeModalProps> = ({ isOpen, onClose }) => {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Şifre Değiştir"
      subtitle="Hesap Güvenliği"
      headerVariant="dark"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button variant="secondary" size="sm" onClick={onClose}>
            Kapat
          </Button>
        </div>
      }
    >
      <div className="space-y-4 py-2 select-none">
        <div className="w-12 h-12 rounded-2xl bg-amber-50 border border-amber-200 text-amber-600 flex items-center justify-center mx-auto">
          <ShieldAlert className="h-6 w-6" />
        </div>
        <div className="text-center space-y-2">
          <p className="text-sm font-semibold text-neutral-800">
            Self-Servis Şifre Değiştirme
          </p>
          <Alert variant="warning">
            Şifre değiştirme işlemi henüz bu platform üzerinden kullanılamıyor. Kurum yöneticinizle iletişime geçin.
          </Alert>
        </div>
      </div>
    </Modal>
  );
};
