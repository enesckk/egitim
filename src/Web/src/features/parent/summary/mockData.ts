import { ParentSummaryViewModel } from './types';

export const initialParentSummaryData: ParentSummaryViewModel = {
  student: {
    id: 'student-1',
    name: 'Ayşe Kaya',
    initials: 'AK',
    grade: '11. Sınıf',
    track: 'Sayısal',
    examFocus: 'AYT',
    coachName: 'Hasan Yılmaz',
    coachTitle: 'Öğrenci Koçu',
  },
  lastUpdated: 'Bugün, 08:30',
  planAdherencePercentage: 82,
  hasActivePlan: true,
  planTitle: 'Haftalık Çalışma Planı',
  completedTasksCount: 14,
  totalTasksCount: 18,
  weeklyActivity: [
    { day: 'Pzt', done: true, hours: 3.5, today: false },
    { day: 'Sal', done: true, hours: 2.5, today: false },
    { day: 'Çar', done: true, hours: 4.0, today: false },
    { day: 'Per', done: false, hours: 0, today: false },
    { day: 'Cum', done: true, hours: 3.0, today: false },
    { day: 'Cmt', done: true, hours: 3.5, today: false },
    { day: 'Paz', done: false, hours: 0, today: true },
  ],
  academicStatus: [
    {
      category: 'Güçlenen Alan',
      categoryType: 'strengthening',
      subjects: ['Matematik', 'Fizik'],
      detail: 'Son iki haftada belirgin ilerleme kaydetti.',
    },
    {
      category: 'Gelişiyor',
      categoryType: 'developing',
      subjects: ['Kimya', 'Tarih'],
      detail: 'Tutarlı çalışma sürüyor, olumlu yönde ilerliyor.',
    },
    {
      category: 'Öncelik',
      categoryType: 'priority',
      subjects: ['Türkçe'],
      detail: 'Bu alanda daha fazla zaman ayrılması önerilmektedir.',
    },
    {
      category: 'Tekrar Öneriliyor',
      categoryType: 'repetition',
      subjects: ['Paragraf Soruları', 'Dil Bilgisi'],
      detail: 'Geçmiş konular üzerinde tekrar yapılması faydalı olacak.',
    },
  ],
  upcomingEvents: [
    {
      id: 'event-1',
      date: '15 Mart, Cuma',
      label: 'TYT Deneme Sınavı 4',
      note: 'Genel Değerlendirme',
    },
    {
      id: 'event-2',
      date: '18 Mart, Pazartesi',
      label: 'Koçluk Görüşmesi',
      note: 'Haftalık Değerlendirme',
    },
  ],
};
