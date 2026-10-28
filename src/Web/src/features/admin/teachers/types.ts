export interface AdminTeacherDirectoryItem {
  id: string;
  name: string;
  firstName: string;
  lastName: string;
  initials: string;
  title?: string | null;
  assignedClassrooms: Array<{ id: string; name: string; isPrimary: boolean; studentCount: number }>;
  assignedSubjectIds: string[];
  totalStudentCount: number;
  status: 'active' | 'inactive';
}

export interface AdminTeachersViewModel {
  teachers: AdminTeacherDirectoryItem[];
}
