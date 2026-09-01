export interface AdminTeacherDirectoryItem {
  id: string;
  name: string;
  initials: string;
  subject: string;
  assignedClasses: string[];
  totalStudentCount: number;
  branch: string;
  status: 'active' | 'inactive';
}

export interface AdminTeachersViewModel {
  teachers: AdminTeacherDirectoryItem[];
}
