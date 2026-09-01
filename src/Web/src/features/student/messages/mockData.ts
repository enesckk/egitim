import { StudentMessagesViewModel } from './types';

export const initialStudentMessagesData: StudentMessagesViewModel = {
  conversationId: 'conv-coach-hasan',
  unreadCount: 0,
  coach: {
    id: 'coach-01',
    name: 'Hasan Yılmaz',
    role: 'coach',
    title: 'Kıdemli YKS Koçu',
    institution: 'Bilim Akademi • Kadıköy Şubesi',
    avatarInitials: 'HY',
    isOnline: true,
    nextMeetingDate: 'Cuma, 16:00',
  },
  messages: [
    {
      id: 'msg-01',
      senderId: 'coach-01',
      senderName: 'Hasan Yılmaz',
      senderRole: 'coach',
      content: 'Selam Ayşe, son Özdebir TYT deneme sonucunu inceledim. Matematik ve Türkçe netlerindeki istikrarlı artış çok iyi gidiyor.',
      timestamp: 'Dün 14:15',
      isRead: true,
    },
    {
      id: 'msg-02',
      senderId: 'student-01',
      senderName: 'Ayşe Kaya',
      senderRole: 'student',
      content: 'Teşekkürler hocam! Fen kısmında Fizik Optik sorularında biraz süre kaybettim. Limit çalışmasına da başladım.',
      timestamp: 'Dün 14:32',
      isRead: true,
    },
    {
      id: 'msg-03',
      senderId: 'coach-01',
      senderName: 'Hasan Yılmaz',
      senderRole: 'coach',
      content: 'Optik için bu haftaki çalışma planına ek 30 dakikalık odak konu tekrarı koydum. Cuma günkü görüşmemizde de birlikte 2 soru tipini inceleyelim.',
      timestamp: 'Bugün 10:05',
      isRead: true,
    },
  ],
};
