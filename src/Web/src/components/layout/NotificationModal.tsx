import React, { useState, useEffect } from 'react';
import { Bell, Check, Clock, AlertCircle } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Skeleton } from '@/components/ui/Skeleton';
import { notificationService, NotificationDto } from '@/services/notificationService';

export interface NotificationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onNotificationsChanged?: () => void;
}

export const NotificationModal: React.FC<NotificationModalProps> = ({
  isOpen,
  onClose,
  onNotificationsChanged,
}) => {
  const [notifications, setNotifications] = useState<NotificationDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchNotifications = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const items = await notificationService.listNotifications();
      setNotifications(items || []);
    } catch {
      setError('Bildirimler yüklenirken bir sorun oluştu.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      fetchNotifications();
    }
  }, [isOpen]);

  const handleMarkRead = async (id: string) => {
    try {
      await notificationService.markRead(id);
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      );
      if (onNotificationsChanged) onNotificationsChanged();
    } catch {
      // non-blocking
    }
  };

  const handleMarkAllRead = async () => {
    try {
      await notificationService.markAllRead();
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })));
      if (onNotificationsChanged) onNotificationsChanged();
    } catch {
      // non-blocking
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Bildirimler"
      subtitle="Sistem, Deneme ve Koçluk Bildirimleri"
      headerVariant="dark"
      footer={
        <div className="flex justify-between items-center w-full">
          {notifications.some((n) => !n.isRead) ? (
            <Button
              variant="outline"
              size="sm"
              onClick={handleMarkAllRead}
              id="btn-mark-all-read"
            >
              Tümünü Okundu Say
            </Button>
          ) : (
            <div />
          )}
          <Button variant="secondary" size="sm" onClick={onClose}>
            Kapat
          </Button>
        </div>
      }
    >
      <div className="space-y-3 py-1 select-none max-h-96 overflow-y-auto">
        {isLoading ? (
          <div className="space-y-2 p-2">
            <Skeleton className="h-16 w-full rounded-xl" />
            <Skeleton className="h-16 w-full rounded-xl" />
            <Skeleton className="h-16 w-full rounded-xl" />
          </div>
        ) : error ? (
          <div className="p-4 text-center text-xs text-danger flex items-center justify-center gap-1.5">
            <AlertCircle className="h-4 w-4" />
            <span>{error}</span>
          </div>
        ) : notifications.length === 0 ? (
          <div className="py-8 text-center text-xs text-neutral-400">
            <Bell className="h-8 w-8 text-neutral-300 mx-auto mb-2" />
            <p>Henüz bir bildiriminiz bulunmuyor.</p>
          </div>
        ) : (
          <div className="divide-y divide-neutral-100" id="notification-list">
            {notifications.map((n) => (
              <div
                key={n.id}
                className={`py-3 px-2 flex items-start justify-between gap-3 transition-colors ${
                  !n.isRead ? 'bg-primary-50/40 rounded-xl' : ''
                }`}
              >
                <div className="space-y-1 flex-1">
                  <div className="flex items-center gap-2">
                    <span className="font-semibold text-xs text-neutral-900">
                      {n.title}
                    </span>
                    {!n.isRead && (
                      <span className="w-2 h-2 rounded-full bg-primary-600 inline-block" />
                    )}
                  </div>
                  <p className="text-xs text-neutral-600 leading-relaxed">
                    {n.message}
                  </p>
                  <div className="flex items-center gap-1 text-[10px] text-neutral-400 pt-0.5">
                    <Clock className="h-3 w-3" />
                    <span>
                      {new Date(n.createdAt).toLocaleDateString('tr-TR', {
                        day: 'numeric',
                        month: 'short',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </span>
                  </div>
                </div>

                {!n.isRead && (
                  <button
                    type="button"
                    onClick={() => handleMarkRead(n.id)}
                    className="p-1.5 text-neutral-400 hover:text-primary-600 hover:bg-neutral-100 rounded-lg transition-colors"
                    title="Okundu İşaretle"
                    aria-label="Okundu İşaretle"
                  >
                    <Check className="h-4 w-4" />
                  </button>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </Modal>
  );
};
