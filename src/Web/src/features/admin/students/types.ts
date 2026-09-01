export type StudentStatus = 'takipte' | 'dikkat' | 'kritik';

export interface AdminStudentDirectoryItem {
  id: string;
  name: string;
  initials: string;
  grade: string;
  exam: string;
  field: string;
  status: StudentStatus;
  planAdherence: number;
  lastActivity: string;
  lastActivityDays: number;
  coach: string;
  branch: string;
}

export interface AdminStudentFilterState {
  search: string;
  grade: string;
  exam: string;
  field: string;
  status: string;
  adherence: string;
  activity: string;
  coach: string;
  branch: string;
}

export interface AdminStudentDirectoryViewModel {
  students: AdminStudentDirectoryItem[];
}
