import { apiClient } from './api/apiClient';

export interface NotificationDto {
  id: string;
  institutionId: string;
  userId: string;
  type: number;
  title: string;
  message: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  isRead: boolean;
  readAt?: string;
  createdAt: string;
}

export const notificationService = {
  listNotifications: async (isRead?: boolean): Promise<NotificationDto[]> => {
    return apiClient.get<NotificationDto[]>('/api/v1/notifications', {
      params: isRead !== undefined ? { isRead } : undefined,
    });
  },

  getUnreadCount: async (): Promise<number> => {
    const res = await apiClient.get<{ count: number }>('/api/v1/notifications/unread-count');
    return res.count;
  },

  markRead: async (id: string): Promise<void> => {
    await apiClient.patch(`/api/v1/notifications/${id}/read`);
  },

  markAllRead: async (): Promise<void> => {
    await apiClient.post('/api/v1/notifications/read-all');
  },
};
