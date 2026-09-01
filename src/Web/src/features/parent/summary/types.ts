export type AcademicCategoryType = 'strengthening' | 'developing' | 'priority' | 'repetition';

export interface AcademicStatusItem {
  category: string;
  categoryType: AcademicCategoryType;
  subjects: string[];
  detail: string;
}

export interface DayActivityItem {
  day: string;
  done: boolean;
  hours: number;
  today?: boolean;
}

export interface UpcomingEventItem {
  id: string;
  date: string;
  label: string;
  note: string;
}

export interface LinkedStudentSummary {
  id: string;
  name: string;
  initials: string;
  grade: string;
  track: string;
  examFocus: string;
  coachName: string;
  coachTitle: string;
}

export interface ParentSummaryViewModel {
  student: LinkedStudentSummary;
  lastUpdated: string;
  weeklyActivity: DayActivityItem[];
  planAdherencePercentage: number;
  academicStatus: AcademicStatusItem[];
  upcomingEvents: UpcomingEventItem[];
}
