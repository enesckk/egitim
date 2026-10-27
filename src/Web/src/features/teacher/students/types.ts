export interface TeacherStudentTopicPerformance {
  id: string;
  subject: string;
  topic: string;
  masteryPercentage: number;
  solvedQuestionCount: number;
  accuracyRate: number;
  status: 'mastered' | 'improving' | 'needs_work';
  lastPracticedDate?: string;
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

export interface TeacherStudentAcademicGap {
  id: string;
  subject: string;
  topic: string;
  severity: 'critical' | 'warning' | 'info';
  detail: string;
  lastExam?: string;
}

export interface TeacherStudentSubjectSummary {
  subjectName: string;
  totalNet?: number | null;
  accuracy?: number | null;
  examCount?: number;
}

export interface TeacherStudentDetailModel {
  id: string;
  name: string;
  initials: string;
  grade: string;
  classBranch: string;
  studentNumber?: string | null;
  assignedSubjects: string[];
  mathAverageNet?: number | null;
  physicsAverageNet?: number | null;
  overallAcademicStatus: 'takipte' | 'dikkat' | 'kritik';
  topicPerformances: TeacherStudentTopicPerformance[];
  recentExamResults: TeacherStudentExamDetail[];
  academicGaps?: TeacherStudentAcademicGap[];
  subjectSummaries?: TeacherStudentSubjectSummary[];
}

export interface TeacherStudentsViewModel {
  students: TeacherStudentDetailModel[];
}
