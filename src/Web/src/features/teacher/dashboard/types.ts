export type AcademicSeverity = 'critical' | 'warning' | 'info';

export interface AcademicGapItem {
  id: string;
  studentId: string;
  studentName: string;
  grade: string;
  subject: string;
  topic: string;
  detail: string;
  severity: AcademicSeverity;
  initials: string;
  lastExam: string;
}

export interface TeacherStudentNetItem {
  id: string;
  studentId: string;
  name: string;
  grade: string;
  mathNet: number | null;
  physicsNet: number | null;
  trend: 'up' | 'stable' | 'down';
  initials: string;
}

export interface TeacherDashboardViewModel {
  teacherName: string;
  assignedSubjects: string[];
  totalAssignedStudents: number;
  totalAssignedClasses: number;
  academicGaps: AcademicGapItem[];
  studentNets: TeacherStudentNetItem[];
}
