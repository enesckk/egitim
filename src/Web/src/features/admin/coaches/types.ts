import { CoachAssignedStudentDto } from '@/services/adminService';

export interface AdminCoachDirectoryItem {
  id: string;
  name: string;
  firstName: string;
  lastName: string;
  initials: string;
  institutionId: string;
  assignedStudentCount: number;
  assignedStudents: CoachAssignedStudentDto[];
  status: 'active' | 'inactive';
}

export interface AdminCoachesViewModel {
  coaches: AdminCoachDirectoryItem[];
}
