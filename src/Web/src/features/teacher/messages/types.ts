export interface TeacherMessageItem {
  id: string;
  senderId: string;
  senderName: string;
  senderRole: 'teacher' | 'student';
  content: string;
  timestamp: string;
}

export interface TeacherConversation {
  id: string;
  studentId: string;
  studentName: string;
  studentInitials: string;
  studentGrade: string;
  subject: string;
  lastMessage: string;
  lastMessageTime: string;
  unreadCount: number;
  messages: TeacherMessageItem[];
}

export interface TeacherMessagesViewModel {
  conversations: TeacherConversation[];
}
