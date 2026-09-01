import React, { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';

export interface PasswordChangeModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const PasswordChangeModal: React.FC<PasswordChangeModalProps> = ({ isOpen, onClose }) => {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [isSuccess, setIsSuccess] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!currentPassword || !newPassword || !confirmPassword) {
      setError('Lütfen tüm alanları doldurunuz.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setError('Yeni şifreler birbiriyle eşleşmiyor.');
      return;
    }
    if (newPassword.length < 6) {
      setError('Yeni şifre en az 6 karakter olmalıdır.');
      return;
    }

    setError('');
    setIsSuccess(true);
    setTimeout(() => {
      setIsSuccess(false);
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
      onClose();
    }, 1500);
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Şifre Değiştir"
      subtitle="Hesap güvenliğiniz için şifrenizi güncelleyin"
      headerVariant="dark"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button variant="secondary" size="sm" onClick={onClose}>
            İptal
          </Button>
          <Button variant="primary" size="sm" onClick={handleSubmit}>
            Şifreyi Güncelle
          </Button>
        </div>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-3.5 select-none">
        {error && <Alert variant="danger">{error}</Alert>}
        {isSuccess && <Alert variant="success">Şifreniz başarıyla güncellendi!</Alert>}

        <Input
          type="password"
          label="Mevcut Şifre"
          placeholder="••••••••"
          value={currentPassword}
          onChange={(e) => setCurrentPassword(e.target.value)}
        />

        <Input
          type="password"
          label="Yeni Şifre"
          placeholder="En az 6 karakter"
          value={newPassword}
          onChange={(e) => setNewPassword(e.target.value)}
        />

        <Input
          type="password"
          label="Yeni Şifre (Tekrar)"
          placeholder="Yeni şifrenizi doğrulayın"
          value={confirmPassword}
          onChange={(e) => setConfirmPassword(e.target.value)}
        />
      </form>
    </Modal>
  );
};
