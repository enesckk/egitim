export type MaterialType = 'fasikul' | 'test' | 'soru_bankasi' | 'deneme';

export interface TeacherContentItem {
  id: string;
  title: string;
  subject: string;
  topic: string;
  grade: string;
  type: MaterialType;
  questionCount?: number;
  pageCount?: number;
  durationMinutes?: number;
  assignedClassesCount: number;
}

export interface TeacherContentViewModel {
  materials: TeacherContentItem[];
}
