import React from 'react';
import { LogOut } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';

export interface LogoutConfirmModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export const LogoutConfirmModal: React.FC<LogoutConfirmModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
}) => {
  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Çıkış Yap"
      subtitle="Oturumunuzu sonlandırmak istediğinizden emin misiniz?"
      headerVariant="dark"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button variant="secondary" size="sm" onClick={onClose}>
            Vazgeç
          </Button>
          <Button
            variant="destructive"
            size="sm"
            onClick={onConfirm}
            leftIcon={<LogOut className="h-4 w-4" />}
          >
            Çıkış Yap
          </Button>
        </div>
      }
    >
      <div className="p-2 select-none text-xs text-neutral-500 leading-relaxed">
        Oturumunuz kapatıldığında tekrar giriş yapmak için öğrenci e-posta ve şifrenizi kullanmanız gerekecektir.
      </div>
    </Modal>
  );
};
