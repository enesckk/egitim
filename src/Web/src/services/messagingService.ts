import { apiClient } from './api/apiClient';

export interface ConversationSummaryDto {
  id: string;
  otherUserId: string;
  otherStudentId?: string | null;
  otherUserName: string;
  otherUserRole: string;
  subject?: string | null;
  lastMessageSnippet?: string;
  lastMessageAt: string;
  unreadCount: number;
}

export interface RecipientDto {
  userId: string;
  fullName: string;
  role: string;
  studentId?: string | null;
}

export interface MessageDto {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderUserName: string;
  senderRole: string;
  content: string;
  createdAt: string;
  isRead: boolean;
  readAt?: string;
}

export interface SendMessageRequest {
  conversationId?: string;
  recipientUserId?: string;
  content: string;
  subject?: string;
}

export const messagingService = {
  async listConversations(): Promise<ConversationSummaryDto[]> {
    return apiClient.get<ConversationSummaryDto[]>('/api/v1/communication/messages/conversations');
  },

  async getRecipients(): Promise<RecipientDto[]> {
    return apiClient.get<RecipientDto[]>('/api/v1/communication/messages/recipients');
  },

  async getConversationMessages(conversationId: string, page = 1, pageSize = 200): Promise<MessageDto[]> {
    return apiClient.get<MessageDto[]>(
      `/api/v1/communication/messages/conversations/${conversationId}?page=${page}&pageSize=${pageSize}`
    );
  },

  async getTotalUnreadCount(): Promise<{ count: number }> {
    return apiClient.get<{ count: number }>('/api/v1/communication/messages/unread-count');
  },

  async markConversationRead(conversationId: string): Promise<void> {
    return apiClient.patch<void>(`/api/v1/communication/messages/conversations/${conversationId}/read`, {});
  },

  async sendMessage(request: SendMessageRequest): Promise<MessageDto> {
    return apiClient.post<MessageDto>('/api/v1/communication/messages', request);
  },
};
