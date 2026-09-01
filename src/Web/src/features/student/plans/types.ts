export type PlanTaskStatus = 'completed' | 'in_progress' | 'pending';

export interface PlanTaskItem {
  id: string;
  dayIndex: number; // 0: Pzt, 1: Sal, ..., 6: Paz
  subject: string;
  topic: string;
  durationMinutes: number;
  status: PlanTaskStatus;
  isCoachAssigned?: boolean;
  coachNote?: string;
  targetQuestionCount?: number;
  completedAt?: string;
}

export interface DayScheduleSummary {
  dayIndex: number;
  dayName: string;
  dateStr: string;
  taskCount: number;
  completedCount: number;
  isToday?: boolean;
}

export interface SubjectPlanProgress {
  subject: string;
  completedTasks: number;
  totalTasks: number;
  completedHours: number;
  colorClass: string;
}

export interface StudentPlanViewModel {
  planId: string;
  title: string;
  academicGoal: string;
  coachName: string;
  coachAvatarInitials: string;
  currentWeek: string;
  dateRange: string;
  completionRate: number;
  totalTasks: number;
  completedTasks: number;
  days: DayScheduleSummary[];
  tasks: PlanTaskItem[];
  subjectProgress: SubjectPlanProgress[];
}
