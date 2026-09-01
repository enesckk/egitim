export type StudentStatus = 'takipte' | 'dikkat' | 'kritik';

export interface CoachPrivateNote {
  id: string;
  createdAt: string;
  updatedAt?: string;
  category: 'Akademik' | 'Motivasyon' | 'Veli Görüşmesi' | 'Genel';
  content: string;
}

export interface StudentPlanTaskReview {
  id: string;
  dayName: string;
  subject: string;
  topic: string;
  durationMinutes: number;
  status: 'completed' | 'in_progress' | 'pending';
  isCoachAssigned?: boolean;
  coachNote?: string;
}

export interface StudentExamReviewItem {
  id: string;
  title: string;
  type: 'TYT' | 'AYT';
  date: string;
  totalNet: number;
  netChange: number;
  subjectsSummary: string; // e.g. "Mat: 33.25 | Tr: 35.00 | Fen: 16.50"
  weakTopics: string[];
}

export interface AssignedStudentDetail {
  id: string;
  name: string;
  initials: string;
  grade: string;
  field: string;
  exam: string;
  status: StudentStatus;
  planAdherence: number;
  weeklyStudyHours: number;
  lastActivity: string;
  privateNotes: CoachPrivateNote[];
  weeklyPlanTasks: StudentPlanTaskReview[];
  recentExams: StudentExamReviewItem[];
}

export interface CoachStudentsViewModel {
  students: AssignedStudentDetail[];
}
