export interface AdminClassDirectoryItem {
  id: string;
  name: string;
  grade: string;
  field: string;
  studentCount: number;
  assignedTeachers: string[];
  averageAdherencePercentage: number;
  averageNet: number;
  branch: string;
}

export interface AdminClassesViewModel {
  classes: AdminClassDirectoryItem[];
}
