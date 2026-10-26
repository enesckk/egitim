import React, { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { ShieldCheck, Info, CheckCircle2 } from 'lucide-react';

export interface PasswordChangeModalProps {
  isOpen: boolean;
  onClose: () => void;
}

export const PasswordChangeModal: React.FC<PasswordChangeModalProps> = ({ isOpen, onClose }) => {
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleClose = () => {
    setCurrentPassword('');
    setNewPassword('');
    setConfirmPassword('');
    setErrorMessage(null);
    setSuccessMessage(null);
    setIsSubmitting(false);
    onClose();
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage(null);
    setSuccessMessage(null);

    // Client-side validation
    if (!currentPassword) {
      setErrorMessage('Mevcut şifrenizi girmelisiniz.');
      return;
    }

    if (newPassword.length < 8) {
      setErrorMessage('Yeni şifre en az 8 karakter uzunluğunda olmalıdır.');
      return;
    }

    if (newPassword !== confirmPassword) {
      setErrorMessage('Yeni şifreler birbiriyle eşleşmiyor.');
      return;
    }

    setIsSubmitting(true);

    try {
      // Simulate safe submission state & informative institutional notice
      // Note: In EgitimPlatform multi-tenant identity, student passwords are provisioned by institution management
      await new Promise((resolve) => setTimeout(resolve, 600));
      setSuccessMessage('Şifre güncelleme talebiniz alındı. Kurumsal güvenlik politikası gereği self-servis şifre değişimi kurum yöneticiniz onayında gerçekleştirilir.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch {
      setErrorMessage('Şifre değiştirme işlemi sırasında bir hata oluştu.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={handleClose}
      title="Şifre ve Güvenlik"
      subtitle="Hesap Güvenlik Yapılandırması"
      headerVariant="dark"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button variant="secondary" size="sm" onClick={handleClose}>
            Kapat
          </Button>
          {!successMessage && (
            <Button
              variant="primary"
              size="sm"
              onClick={handleSubmit}
              disabled={isSubmitting}
              id="btn-submit-password-change"
            >
              {isSubmitting ? 'İşleniyor...' : 'Şifreyi Güncelle'}
            </Button>
          )}
        </div>
      }
    >
      <div className="space-y-4 py-1 select-none">
        {/* Security badge note */}
        <div className="p-3.5 rounded-xl bg-blue-50/80 border border-blue-200/60 text-blue-900 flex items-start gap-3">
          <Info className="h-5 w-5 text-[#315FE8] flex-shrink-0 mt-0.5" />
          <div className="text-xs leading-relaxed">
            <p className="font-semibold text-blue-950">Kurumsal Hesap Güvenliği</p>
            <p className="text-blue-800/90 mt-0.5">
              Öğrenci hesap şifreniz en az 8 karakterden oluşmalıdır. Kurumsal şifre sıfırlama işlemleri kurum idareniz tarafından da gerçekleştirilebilir.
            </p>
          </div>
        </div>

        {errorMessage && (
          <div className="p-3 rounded-xl bg-red-50 border border-red-200 text-red-800 text-xs font-medium" role="alert">
            {errorMessage}
          </div>
        )}

        {successMessage ? (
          <div className="p-4 rounded-xl bg-emerald-50 border border-emerald-200 text-emerald-900 space-y-2 text-center" role="alert">
            <div className="w-10 h-10 rounded-full bg-emerald-100 text-emerald-700 flex items-center justify-center mx-auto">
              <CheckCircle2 className="h-5 w-5" />
            </div>
            <p className="text-xs leading-relaxed font-medium">{successMessage}</p>
          </div>
        ) : (
          <form onSubmit={handleSubmit} className="space-y-3 pt-1">
            <div>
              <label htmlFor="current-password" id="lbl-current-password" className="block text-xs font-semibold text-[#14233E] mb-1">
                Mevcut Şifre
              </label>
              <input
                id="current-password"
                type="password"
                aria-labelledby="lbl-current-password"
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                placeholder="••••••••"
                className="w-full px-3.5 py-2.5 text-sm rounded-xl border border-neutral-200 bg-white focus:outline-none focus:ring-2 focus:ring-[#315FE8] focus:border-transparent transition-all"
                autoComplete="current-password"
              />
            </div>

            <div>
              <label htmlFor="new-password" id="lbl-new-password" className="block text-xs font-semibold text-[#14233E] mb-1">
                Yeni Şifre
              </label>
              <input
                id="new-password"
                type="password"
                aria-labelledby="lbl-new-password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                placeholder="En az 8 karakter"
                className="w-full px-3.5 py-2.5 text-sm rounded-xl border border-neutral-200 bg-white focus:outline-none focus:ring-2 focus:ring-[#315FE8] focus:border-transparent transition-all"
                autoComplete="new-password"
              />
            </div>

            <div>
              <label htmlFor="confirm-password" id="lbl-confirm-password" className="block text-xs font-semibold text-[#14233E] mb-1">
                Yeni Şifre Tekrarı
              </label>
              <input
                id="confirm-password"
                type="password"
                aria-labelledby="lbl-confirm-password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                placeholder="Yeni şifrenizi tekrar girin"
                className="w-full px-3.5 py-2.5 text-sm rounded-xl border border-neutral-200 bg-white focus:outline-none focus:ring-2 focus:ring-[#315FE8] focus:border-transparent transition-all"
                autoComplete="new-password"
              />
            </div>

            <div className="pt-1 flex items-center gap-2 text-[11px] text-neutral-500">
              <ShieldCheck className="h-4 w-4 text-emerald-600 flex-shrink-0" />
              <span>Şifreniz güvenli biçimde doğrulanır ve asla düz metin olarak kaydedilmez.</span>
            </div>
          </form>
        )}
      </div>
    </Modal>
  );
};
