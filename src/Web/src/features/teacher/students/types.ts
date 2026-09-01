export interface TeacherStudentTopicPerformance {
  id: string;
  subject: string;
  topic: string;
  masteryPercentage: number;
  solvedQuestionCount: number;
  accuracyRate: number; // e.g. 74 (%)
  status: 'mastered' | 'improving' | 'needs_work';
  lastPracticedDate: string;
}

export interface TeacherStudentExamDetail {
  id: string;
  examTitle: string;
  date: string;
  subject: string;
  correctCount: number;
  incorrectCount: number;
  emptyCount: number;
  net: number;
  netChange: number;
  incorrectTopics: string[];
}

export interface TeacherStudentDetailModel {
  id: string;
  name: string;
  initials: string;
  grade: string;
  classBranch: string;
  assignedSubjects: string[];
  mathAverageNet: number;
  physicsAverageNet: number;
  overallAcademicStatus: 'takipte' | 'dikkat' | 'kritik';
  topicPerformances: TeacherStudentTopicPerformance[];
  recentExamResults: TeacherStudentExamDetail[];
}

export interface TeacherStudentsViewModel {
  students: TeacherStudentDetailModel[];
}
