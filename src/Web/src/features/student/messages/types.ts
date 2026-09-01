export interface MessageItem {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: 'coach' | 'student';
  content: string;
  timestamp: string;
  isRead: boolean;
}

export interface ConversationParticipant {
  id: string;
  name: string;
  role: 'coach' | 'student';
  title: string;
  institution: string;
  avatarInitials: string;
  isOnline: boolean;
  nextMeetingDate?: string;
}

export interface StudentMessagesViewModel {
  conversationId: string;
  coach: ConversationParticipant;
  messages: MessageItem[];
  unreadCount: number;
}
