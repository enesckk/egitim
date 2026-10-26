export interface ClassStudentItem {
  id: string;
  studentId: string;
  name: string;
  initials: string;
  mathNet: number | null;
  physicsNet: number | null;
  status: 'takipte' | 'dikkat' | 'kritik';
  trend: 'up' | 'stable' | 'down';
  weakTopic?: string;
}

export interface ClassTopicMasteryItem {
  topic: string;
  subject: string;
  masteryPercentage: number;
  unmasteredStudentCount: number;
}

export interface TeacherClassDetail {
  id: string;
  name: string;
  grade: string;
  branch: string;
  subject: string;
  studentCount: number;
  averageNet?: number | null;
  attentionRequiredCount: number;
  students: ClassStudentItem[];
  topicMasteries: ClassTopicMasteryItem[];
  recentExamTitle?: string | null;
  recentExamAverageNet?: number | null;
}

export interface TeacherClassesViewModel {
  classes: TeacherClassDetail[];
}
