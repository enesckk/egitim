export type StudentStatus = 'Active' | 'Inactive';

export interface AdminStudentDirectoryItem {
  id: string;
  name: string;
  initials: string;
  grade: string;
  gradeLevel?: number | null;
  exam: string;
  schoolName?: string | null;
  studentNumber?: string | null;
  status: string;
  enrollmentDate?: string | null;
  createdAt: string;
}

export interface AdminStudentFilterState {
  search: string;
  grade: string;
  status: string;
}

export interface AdminStudentDirectoryViewModel {
  students: AdminStudentDirectoryItem[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}
