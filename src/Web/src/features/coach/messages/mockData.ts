import { CoachMessagesViewModel } from './types';

export const initialCoachMessagesData: CoachMessagesViewModel = {
  conversations: [
    {
      id: 'conv-1',
      studentId: 'student-1',
      studentName: 'Ayşe Kaya',
      studentInitials: 'AK',
      studentGrade: '11. Sınıf • Sayısal',
      lastMessage: 'Optik için bu haftaki çalışma planına ek 30 dakikalık odak konu tekrarı koydum.',
      lastMessageTime: '10:05',
      unreadCount: 0,
      messages: [
        {
          id: 'm-1',
          senderId: 'coach-1',
          senderName: 'Hasan Yılmaz',
          senderRole: 'coach',
          content: 'Selam Ayşe, son Özdebir TYT deneme sonucunu inceledim. Matematik ve Türkçe netlerindeki istikrarlı artış çok iyi gidiyor.',
          timestamp: 'Dün 14:15',
        },
        {
          id: 'm-2',
          senderId: 'student-1',
          senderName: 'Ayşe Kaya',
          senderRole: 'student',
          content: 'Teşekkürler hocam! Fen kısmında Fizik Optik sorularında biraz süre kaybettim. Limit çalışmasına da başladım.',
          timestamp: 'Dün 14:32',
        },
        {
          id: 'm-3',
          senderId: 'coach-1',
          senderName: 'Hasan Yılmaz',
          senderRole: 'coach',
          content: 'Optik için bu haftaki çalışma planına ek 30 dakikalık odak konu tekrarı koydum. Cuma günkü görüşmemizde de birlikte 2 soru tipini inceleyelim.',
          timestamp: 'Bugün 10:05',
        },
      ],
    },
    {
      id: 'conv-2',
      studentId: 'student-3',
      studentName: 'Zeynep Demir',
      studentInitials: 'ZD',
      studentGrade: '12. Sınıf • Sayısal',
      lastMessage: 'Hocam bugünkü görüşme saatini 17:30 yapabilir miyiz?',
      lastMessageTime: 'Dün',
      unreadCount: 1,
      messages: [
        {
          id: 'm-4',
          senderId: 'student-3',
          senderName: 'Zeynep Demir',
          senderRole: 'student',
          content: 'Hocam bugünkü görüşme saatini 17:30 yapabilir miyiz? Okul çıkışı biraz uzadı.',
          timestamp: 'Dün 18:20',
        },
      ],
    },
    {
      id: 'conv-3',
      studentId: 'student-4',
      studentName: 'Ali Çelik',
      studentInitials: 'AÇ',
      studentGrade: '11. Sınıf • EA',
      lastMessage: 'Geometri ödevini tamamladım hocam.',
      lastMessageTime: '2 gün önce',
      unreadCount: 0,
      messages: [
        {
          id: 'm-5',
          senderId: 'student-4',
          senderName: 'Ali Çelik',
          senderRole: 'student',
          content: 'Geometri ödevini tamamladım hocam.',
          timestamp: '2 gün önce',
        },
      ],
    },
  ],
};
