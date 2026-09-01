import { StudentTodayViewModel } from './types';

export const initialStudentTodayData: StudentTodayViewModel = {
  studentName: 'Ayşe',
  dateString: new Date().toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  }),
  nextStudy: {
    title: 'TYT Analizi',
    topic: 'Kasım Denemesi Çözüm',
    durationMinutes: 45,
    subject: 'TYT Analizi',
  },
  todayPlans: [
    {
      id: 1,
      subject: 'Matematik',
      topic: 'Limit ve Süreklilik',
      durationMinutes: 90,
      status: 'completed',
      completedAt: '09:30',
    },
    {
      id: 2,
      subject: 'TYT Analizi',
      topic: 'Kasım Denemesi Çözüm',
      durationMinutes: 45,
      status: 'active',
    },
    {
      id: 3,
      subject: 'Türkçe',
      topic: 'Sözcük Türleri — Tekrar',
      durationMinutes: 60,
      status: 'upcoming',
    },
    {
      id: 4,
      subject: 'Felsefe',
      topic: 'Ahlak Felsefesi Özeti',
      durationMinutes: 30,
      status: 'upcoming',
    },
  ],
  weeklyProgress: {
    days: [
      { label: 'Pzt', done: true },
      { label: 'Sal', done: true },
      { label: 'Çar', done: true },
      { label: 'Per', done: false },
      { label: 'Cum', done: true },
      { label: 'Cmt', done: true },
      { label: 'Paz', done: false, today: true },
    ],
    completedHours: 18,
    targetHours: 21,
    remainingHours: 3,
    adherencePercentage: 85.7,
    subjects: [
      { subject: 'Matematik', hours: 6.5, colorClass: 'bg-primary-500' },
      { subject: 'Türkçe', hours: 4, colorClass: 'bg-success' },
      { subject: 'Fizik', hours: 3.5, colorClass: 'bg-attention' },
      { subject: 'Kimya', hours: 2.5, colorClass: 'bg-warning' },
      { subject: 'Diğer', hours: 1.5, colorClass: 'bg-neutral-300' },
    ],
  },
  recommendation: {
    topic: 'Limit & Süreklilik',
    message:
      'Limit konusunda son 2 denemede tekrarlayan yanlışlar var. Bu konuyu bugün pekiştirmeni öneririz.',
    actionLabel: 'Limit Çalışması Başlat →',
  },
  upcomingEvents: [
    { id: 1, date: '5 Kasım', label: 'TYT Denemesi', type: 'exam' },
    { id: 2, date: '8 Kasım', label: 'Koç Görüşmesi — Hasan Bey', type: 'session' },
    { id: 3, date: '12 Kasım', label: 'AYT Denemesi', type: 'exam' },
  ],
  monthlyStats: [
    { label: 'Çalışma Günü', value: '22', type: 'award' },
    { label: 'Saat', value: '86', type: 'clock' },
    { label: 'Deneme', value: '4', type: 'trend' },
    { label: 'Net Art.', value: '+4.2', type: 'trend' },
  ],
};
