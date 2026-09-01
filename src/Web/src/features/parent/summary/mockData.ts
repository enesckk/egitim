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
  weeklyActivity: [
    { day: 'Pzt', done: true, hours: 3.5 },
    { day: 'Sal', done: true, hours: 2.5 },
    { day: 'Çar', done: true, hours: 4.0 },
    { day: 'Per', done: false, hours: 0 },
    { day: 'Cum', done: true, hours: 3.0 },
    { day: 'Cmt', done: true, hours: 3.5 },
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
      id: 'up-1',
      date: '5 Kasım 2026',
      label: 'TYT Denemesi',
      note: 'Okul bazlı deneme',
    },
    {
      id: 'up-2',
      date: '8 Kasım 2026',
      label: 'Koç Görüşmesi',
      note: 'Hasan Bey ile',
    },
  ],
};
