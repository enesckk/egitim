import { CoachReportsViewModel } from './types';

export const initialCoachReportsData: CoachReportsViewModel = {
  portfolioOverallAdherence: 82,
  portfolioAverageStudyHours: 19.4,
  totalAssignedStudents: 24,
  atRiskStudentsCount: 3,
  gradeBreakdown: [
    { grade: '12. Sınıf (AYT)', adherencePercentage: 79, studentCount: 10, averageHours: 24.2 },
    { grade: '11. Sınıf (YKS Hazırlık)', adherencePercentage: 86, studentCount: 8, averageHours: 18.5 },
    { grade: '10. Sınıf (TYT Temel)', adherencePercentage: 81, studentCount: 6, averageHours: 14.0 },
  ],
  attentionCategories: [
    {
      category: 'Devamsızlık / Giriş Yapmama',
      count: 2,
      description: 'Son 4 günden uzun süredir platforma giriş yapmayan öğrenciler.',
      colorClass: 'bg-danger',
    },
    {
      category: 'Düşük Plan Tamamlama (<%50)',
      count: 2,
      description: 'Haftalık hedeflerinin yarısından azını tamamlayan öğrenciler.',
      colorClass: 'bg-warning',
    },
    {
      category: 'Deneme Net Düşüşü',
      count: 1,
      description: 'Son denemede önceki sınava göre 5+ net gerileyen öğrenciler.',
      colorClass: 'bg-attention',
    },
  ],
};
