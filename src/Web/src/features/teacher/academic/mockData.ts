import { TeacherAcademicViewModel } from './types';

export const initialTeacherAcademicData: TeacherAcademicViewModel = {
  subjects: ['Matematik', 'Fizik'],
  alerts: [
    {
      id: 'alt-1',
      type: 'repeated_mistake',
      title: 'Limit ve Süreklilikte Kavram Boşluğu',
      description: '11-A şubesinde 8 öğrenci son 2 denemede 0/0 belirsizliği sorularında ardışık hata yaptı.',
      affectedStudentsCount: 8,
      subject: 'Matematik',
      topic: 'Limit ve Süreklilik',
    },
    {
      id: 'alt-2',
      type: 'score_decline',
      title: 'Optik ve Kırılma Net Düşüşü',
      description: '12-B şubesinde optik sorularındaki doğru cevaplama oranı son sınavda %38 geriledi.',
      affectedStudentsCount: 6,
      subject: 'Fizik',
      topic: 'Optik',
    },
  ],
  topics: [
    {
      id: 'topic-1',
      subject: 'Matematik',
      topicName: 'Limit ve Süreklilik',
      grade: '11. Sınıf',
      overallMastery: 62,
      totalQuestionsSolved: 840,
      averageAccuracy: 64,
      kazanımlar: [
        { code: 'MAT.11.5.1', description: 'Bir fonksiyonun bir noktadaki limitini açıklar ve özelliklerini kullanır', masteryPercentage: 74, status: 'in_progress', atRiskStudentCount: 4 },
        { code: 'MAT.11.5.2', description: 'Limit ile ilgili belirsizlik durumlarını inceler (0/0)', masteryPercentage: 48, status: 'critical', atRiskStudentCount: 8 },
        { code: 'MAT.11.5.3', description: 'Bir fonksiyonun bir noktadaki sürekliliğini açıklar', masteryPercentage: 66, status: 'in_progress', atRiskStudentCount: 5 },
      ],
    },
    {
      id: 'topic-2',
      subject: 'Matematik',
      topicName: 'Trigonometri',
      grade: '11. Sınıf',
      overallMastery: 84,
      totalQuestionsSolved: 1250,
      averageAccuracy: 88,
      kazanımlar: [
        { code: 'MAT.11.2.1', description: 'Trigonometrik fonksiyonların periyodunu ve grafiğini yorumlar', masteryPercentage: 88, status: 'mastered', atRiskStudentCount: 1 },
        { code: 'MAT.11.2.2', description: 'Toplam, fark ve iki kat açı formüllerini problem çözümlerinde kullanır', masteryPercentage: 80, status: 'mastered', atRiskStudentCount: 3 },
      ],
    },
    {
      id: 'topic-3',
      subject: 'Fizik',
      topicName: 'Optik & Işıkta Kırılma',
      grade: '10. / 11. Sınıf',
      overallMastery: 52,
      totalQuestionsSolved: 620,
      averageAccuracy: 55,
      kazanımlar: [
        { code: 'FİZ.10.4.1', description: 'Işığın saydam ortamlarda kırılma kanunlarını açıklar (Snell Yasası)', masteryPercentage: 50, status: 'critical', atRiskStudentCount: 7 },
        { code: 'FİZ.10.4.2', description: 'Tam yansıma ve sınır açısı olaylarını günlük hayatla ilişkilendirir', masteryPercentage: 54, status: 'critical', atRiskStudentCount: 6 },
      ],
    },
    {
      id: 'topic-4',
      subject: 'Fizik',
      topicName: 'Vektörler & Kuvvet Dengesi',
      grade: '11. Sınıf (AYT)',
      overallMastery: 86,
      totalQuestionsSolved: 780,
      averageAccuracy: 89,
      kazanımlar: [
        { code: 'FİZ.11.1.1', description: 'Vektörlerin bileşkelerini iki ve üç boyutlu kartezyen koordinatlarında hesaplar', masteryPercentage: 88, status: 'mastered', atRiskStudentCount: 2 },
        { code: 'FİZ.11.1.2', description: 'Kesişen kuvvetlerin dengesi şartlarını analiz eder', masteryPercentage: 84, status: 'mastered', atRiskStudentCount: 2 },
      ],
    },
  ],
};
