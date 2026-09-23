export enum ExamStatus {
  Draft = 0,
  Published = 1,
  Closed = 2,
}

export enum AttemptStatus {
  Draft = 0,
  Finalized = 1,
}

export enum ScoreSource {
  None = 0,
  Institution = 1,
  Provider = 2,
}

export interface ExamDto {
  id: string;
  code: string;
  title: string;
  examTypeId: string;
  examTypeName: string;
  examDate: string;
  status: ExamStatus;
  sectionCount: number;
}

export interface ExamSectionDetailDto {
  id: string;
  subjectId: string;
  subjectName: string;
  displayOrder: number;
  questionCount: number;
  effectiveWrongAnswerPenalty: number;
}

export interface ExamDetailDto {
  id: string;
  institutionId: string;
  examTypeId: string;
  examTypeName: string;
  code: string;
  title: string;
  examDate: string;
  status: ExamStatus;
  wrongAnswerPenalty: number;
  scoreSource: ScoreSource;
  sections: ExamSectionDetailDto[];
}

export interface StudentExamResultSummaryDto {
  attemptId: string;
  examId: string;
  examTitle: string;
  examTypeId: string;
  examTypeName: string;
  takenAt: string;
  status: AttemptStatus;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  totalNet: number;
  reportedScore: number | null;
  scoreSource: ScoreSource;
}

export interface TopicResultDetailDto {
  id: string;
  topicId: string;
  topicName: string;
  correct: number;
  wrong: number;
  blank: number;
  net: number;
}

export interface SubjectResultDetailDto {
  id: string;
  examSectionId: string;
  subjectId: string;
  subjectName: string;
  correct: number;
  wrong: number;
  blank: number;
  net: number;
  topics: TopicResultDetailDto[];
}

export interface ExamResultDetailDto {
  attemptId: string;
  examId: string;
  examTitle: string;
  examCode: string;
  examTypeId: string;
  examTypeName: string;
  takenAt: string;
  status: AttemptStatus;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  totalNet: number;
  reportedScore: number | null;
  scoreSource: ScoreSource;
  finalizedAt: string | null;
  rowVersion: string;
  subjectResults: SubjectResultDetailDto[];
}

export interface SubjectAverageDto {
  subjectId: string;
  subjectName: string;
  averageNet: number;
  averageCorrect: number;
  averageWrong: number;
}

export interface WeakTopicDto {
  topicId: string;
  topicName: string;
  subjectId: string;
  subjectName: string;
  totalAttempts: number;
  averageNet: number;
  wrongRatio: number;
}

export interface StudentExamAnalysisDto {
  attemptCount: number;
  averageTotalNet: number;
  latestTotalNet: number;
  previousTotalNet: number;
  delta: number;
  subjectAverages: SubjectAverageDto[];
  weakestTopics: WeakTopicDto[];
}

export interface PaginatedList<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
}
