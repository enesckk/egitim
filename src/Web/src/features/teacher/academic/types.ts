export interface TeacherAcademicGap {
  id: string;
  studentId: string;
  studentName: string;
  grade: string;
  subject: string;
  topic: string;
  detail: string;
  severity: 'critical' | 'warning' | 'info';
  initials: string;
  lastExam?: string;
  classroomId?: string;
}

export interface TeacherAcademicTopicItem {
  id: string;
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  accuracy: number;
  totalQuestions: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  status: 'mastered' | 'improving' | 'needs_work';
  studentName?: string;
  studentId?: string;
  classroomId?: string;
}

export interface TeacherClassroomOption {
  id: string;
  name: string;
  branch?: string;
  gradeLevel?: number;
  studentCount?: number;
}

export interface TeacherAcademicViewModel {
  teacherName: string;
  assignedSubjects: string[];
  totalAssignedClasses: number;
  totalAssignedStudents: number;
  classrooms: TeacherClassroomOption[];
  gaps: TeacherAcademicGap[];
  topicItems: TeacherAcademicTopicItem[];
}
