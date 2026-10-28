export interface AdminClassTeacherItem {
  teacherId: string;
  fullName: string;
  title?: string | null;
  isPrimary: boolean;
}

export interface AdminClassDirectoryItem {
  id: string;
  name: string;
  grade: string;
  gradeLevel?: number | null;
  branch?: string | null;
  field: string;
  academicYear?: string | null;
  studentCount: number;
  teacherCount: number;
  teachers: AdminClassTeacherItem[];
  assignedTeachers: string[];
}

export interface AdminClassesViewModel {
  classes: AdminClassDirectoryItem[];
}
