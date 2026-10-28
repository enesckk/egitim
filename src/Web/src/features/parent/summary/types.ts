export interface LinkedStudentSummary {
  id: string;
  name: string;
  initials: string;
  grade: string;
  track: string;
  examFocus: string;
  coachName: string;
  coachTitle: string;
  studentNumber?: string | null;
  institutionName?: string | null;
}

export interface AcademicStatusItem {
  category: string;
  categoryType: 'strengthening' | 'developing' | 'priority' | 'repetition';
  subjects: string[];
  detail: string;
}

export interface UpcomingEventItem {
  id: string;
  date: string;
  label: string;
  note: string;
  type?: 'exam' | 'task' | 'meeting';
}

export interface DayActivityItem {
  day: string;
  done: boolean;
  hours: number;
  today: boolean;
}

export interface ParentSummaryViewModel {
  student: LinkedStudentSummary;
  lastUpdated: string;
  weeklyActivity: DayActivityItem[];
  planAdherencePercentage: number | null; // null if no active plan
  hasActivePlan: boolean;
  planTitle?: string;
  completedTasksCount?: number;
  totalTasksCount?: number;
  academicStatus: AcademicStatusItem[];
  upcomingEvents: UpcomingEventItem[];
}
