export interface GradeAdherenceItem {
  grade: string;
  adherencePercentage: number;
  studentCount: number;
  averageHours: number;
}

export interface AttentionCategoryItem {
  category: string;
  count: number;
  description: string;
  colorClass: string;
}

export interface CoachReportsViewModel {
  portfolioOverallAdherence: number;
  portfolioAverageStudyHours: number;
  totalAssignedStudents: number;
  atRiskStudentsCount: number;
  gradeBreakdown: GradeAdherenceItem[];
  attentionCategories: AttentionCategoryItem[];
}
