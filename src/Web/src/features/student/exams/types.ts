export type ExamType = 'TYT' | 'AYT' | 'BRANS';

export interface SubjectExamResult {
  subjectName: string;
  correct: number;
  wrong: number;
  empty: number;
  net: number;
  maxNet: number;
  targetNet?: number;
  changeFromPrevious?: number; // e.g. +1.5, -0.5
}

export interface StudentExamItem {
  id: string;
  title: string;
  type: ExamType;
  date: string;
  totalNet: number;
  maxTotalNet: number;
  rankInInstitution?: number;
  totalStudents?: number;
  netChange: number; // e.g. +4.2
  subjects: SubjectExamResult[];
  focusWeakTopics?: string[];
  coachReviewNote?: string;
}

export interface StudentExamsViewModel {
  latestExam: StudentExamItem;
  exams: StudentExamItem[];
  averageNetTYT: number;
  averageNetAYT: number;
  targetNetTYT: number;
  targetNetAYT: number;
}
