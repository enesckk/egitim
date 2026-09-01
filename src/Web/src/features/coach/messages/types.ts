export interface CoachMessageItem {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: 'coach' | 'student';
  content: string;
  timestamp: string;
}

export interface CoachConversation {
  id: string;
  studentId: string;
  studentName: string;
  studentInitials: string;
  studentGrade: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
  messages: CoachMessageItem[];
}

export interface CoachMessagesViewModel {
  conversations: CoachConversation[];
}
