export type AttentionSeverity = 'danger' | 'warning' | 'info';

export interface AttentionStudentItem {
  id: string;
  studentId: string;
  name: string;
  grade: string;
  field: string;
  severity: AttentionSeverity;
  issue: string;
  lastSeen: string;
  ctaLabel: string;
  targetTab?: 'overview' | 'plan' | 'exams' | 'notes';
}

export interface TodaySessionItem {
  id: string;
  studentId: string;
  studentName: string;
  initials: string;
  grade: string;
  type: string;
  time: string;
  duration: string;
  status: 'upcoming' | 'completed' | 'ongoing';
}

export interface PendingActionItem {
  id: string;
  studentId: string;
  studentName: string;
  type: string;
  time: string;
  urgent: boolean;
}

export interface PortfolioSummaryData {
  planAdherenceAvg: number; // e.g. 82
  weeklyStudyHoursAvg: number; // e.g. 19.4
  activeStudentsCount: number; // e.g. 24
  attentionRequiredCount: number; // e.g. 3
}

export interface CoachDashboardViewModel {
  coachName: string;
  dateStr: string;
  attentionStudents: AttentionStudentItem[];
  todaySessions: TodaySessionItem[];
  pendingActions: PendingActionItem[];
  portfolioSummary: PortfolioSummaryData;
}
