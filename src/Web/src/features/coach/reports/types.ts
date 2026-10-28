export interface StudentReportItem {
  id: string;
  fullName: string;
  gradeLevel: number | null;
  hasActivePlan: boolean;
  planTitle?: string;
  planCompletionRate?: number; // 0 to 100
  totalTasks?: number;
  completedTasks?: number;
  coachTasksTotal: number;
  coachTasksCompleted: number;
}

export interface GradeDistributionItem {
  gradeName: string;
  studentCount: number;
  studentsWithPlan: number;
  averageAdherence: number | null; // null if no students have plans
}

export interface CoachReportsViewModel {
  totalAssignedStudents: number;
  activePlansCount: number;
  averagePlanCompletion: number | null; // null if 0 active plans
  totalCoachTasks: number;
  completedCoachTasks: number;
  gradeBreakdown: GradeDistributionItem[];
  studentReports: StudentReportItem[];
}
