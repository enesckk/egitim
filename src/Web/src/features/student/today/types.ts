export type TaskStatus = 'completed' | 'active' | 'upcoming';

export interface TodayPlanItem {
  id: number;
  subject: string;
  topic: string;
  durationMinutes: number;
  status: TaskStatus;
  completedAt?: string;
}

export interface NextStudyItem {
  title: string;
  topic: string;
  durationMinutes: number;
  subject: string;
}

export interface DayStreakItem {
  label: string; // 'Pzt', 'Sal', etc.
  done: boolean;
  today?: boolean;
}

export interface SubjectHourBreakdown {
  subject: string;
  hours: number;
  colorClass: string;
}

export interface WeeklyProgressData {
  days: DayStreakItem[];
  completedHours: number;
  targetHours: number;
  remainingHours: number;
  adherencePercentage: number;
  subjects: SubjectHourBreakdown[];
}

export interface RecommendationItem {
  topic: string;
  message: string;
  actionLabel: string;
}

export interface UpcomingEventItem {
  id: number;
  date: string;
  label: string;
  type: 'exam' | 'session';
}

export interface MonthlyStatItem {
  label: string;
  value: string;
  type: 'award' | 'clock' | 'exam' | 'trend';
}

export interface StudentTodayViewModel {
  studentName: string;
  dateString: string;
  nextStudy: NextStudyItem;
  todayPlans: TodayPlanItem[];
  weeklyProgress: WeeklyProgressData;
  recommendation: RecommendationItem;
  upcomingEvents: UpcomingEventItem[];
  monthlyStats: MonthlyStatItem[];
}
