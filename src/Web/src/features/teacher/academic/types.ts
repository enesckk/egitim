export interface KazanımProgressItem {
  code: string; // e.g. "MAT.11.2.1"
  description: string;
  masteryPercentage: number;
  status: 'mastered' | 'in_progress' | 'critical';
  atRiskStudentCount: number;
}

export interface AcademicTopicSummary {
  id: string;
  subject: string;
  topicName: string;
  grade: string;
  overallMastery: number;
  totalQuestionsSolved: number;
  averageAccuracy: number;
  kazanımlar: KazanımProgressItem[];
}

export interface AcademicAlertItem {
  id: string;
  type: 'repeated_mistake' | 'score_decline' | 'unmastered';
  title: string;
  description: string;
  affectedStudentsCount: number;
  subject: string;
  topic: string;
}

export interface TeacherAcademicViewModel {
  subjects: string[];
  topics: AcademicTopicSummary[];
  alerts: AcademicAlertItem[];
}
