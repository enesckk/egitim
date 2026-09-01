export interface AdminCoachDirectoryItem {
  id: string;
  name: string;
  title: string;
  initials: string;
  assignedStudentCount: number;
  portfolioCapacity: number;
  averageAdherencePercentage: number;
  averageWeeklyStudyHours: number;
  attentionStudentCount: number;
  branch: string;
  status: 'active' | 'inactive';
}

export interface AdminCoachesViewModel {
  coaches: AdminCoachDirectoryItem[];
}
