export type StudentStatus = 'takipte' | 'dikkat' | 'kritik' | 'plansiz';
export type StudentPlanStatus = 'active' | 'no_plan';
export type StudentComplianceStatus = StudentStatus;

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
  subjectsSummary: string;
  weakTopics: string[];
}

export interface AssignedStudentDetail {
  id: string;
  name: string;
  initials: string;
  grade: string;
  field: string;
  schoolName?: string;
  exam: string;
  status: StudentComplianceStatus;
  hasActivePlan?: boolean;
  planAdherence?: number;
  weeklyStudyHours?: number;
  totalTasks?: number;
  completedTasks?: number;
  lastActivity?: string;
  privateNotes: CoachPrivateNote[];
  weeklyPlanTasks: StudentPlanTaskReview[];
  recentExams: StudentExamReviewItem[];
}

export interface CoachStudentsViewModel {
  students: AssignedStudentDetail[];
}
