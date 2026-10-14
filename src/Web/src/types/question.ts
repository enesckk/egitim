export enum QuestionType {
  MultipleChoice = 0,
}

export enum DifficultyLevel {
  Easy = 1,
  Medium = 2,
  Hard = 3,
}

export enum QuestionSourceType {
  InstitutionCustom = 0,
  OsymPast = 1,
  Publisher = 2,
}

export enum QuestionStatus {
  Draft = 0,
  Published = 1,
  Archived = 2,
}

export interface QuestionStyleDto {
  id: string;
  code: string;
  name: string;
  description?: string | null;
  displayOrder?: number;
}

export interface QuestionDto {
  id: string;
  institutionId: string | null;
  subjectId: string;
  topicId: string;
  subTopicId: string | null;
  learningOutcomeId: string | null;
  questionType: QuestionType;
  difficulty: DifficultyLevel;
  sourceType: QuestionSourceType;
  sourceYear: number | null;
  content: string;
  mediaUrl: string | null;
  status: QuestionStatus;
  optionCount: number;
  styles: QuestionStyleDto[];
  createdAt: string;
}

export interface QuestionOptionDetailDto {
  id: string;
  optionLabel: string;
  content: string;
  isCorrect: boolean;
}

export interface QuestionDetailDto {
  id: string;
  institutionId: string | null;
  subjectId: string;
  topicId: string;
  subTopicId: string | null;
  learningOutcomeId: string | null;
  questionType: QuestionType;
  difficulty: DifficultyLevel;
  sourceType: QuestionSourceType;
  sourceYear: number | null;
  content: string;
  mediaUrl: string | null;
  status: QuestionStatus;
  options: QuestionOptionDetailDto[];
  styles: QuestionStyleDto[];
  createdAt: string;
}

export interface GetQuestionsParams {
  subjectId?: string;
  topicId?: string;
  subTopicId?: string;
  learningOutcomeId?: string;
  difficulty?: DifficultyLevel;
  sourceType?: QuestionSourceType;
  sourceYear?: number;
  status?: QuestionStatus;
  searchText?: string;
  poolType?: 'all' | 'global' | 'institution';
  questionStyleId?: string;
  page?: number;
  pageSize?: number;
}

export interface CreateQuestionOptionPayload {
  optionLabel: string;
  content: string;
  isCorrect: boolean;
}

export interface CreateQuestionPayload {
  subjectId: string;
  topicId: string;
  subTopicId?: string | null;
  learningOutcomeId?: string | null;
  questionType: QuestionType;
  difficulty: DifficultyLevel;
  sourceType: QuestionSourceType;
  sourceYear?: number | null;
  content: string;
  mediaUrl?: string | null;
  status: QuestionStatus;
  options: CreateQuestionOptionPayload[];
  questionStyleIds?: string[];
}

export interface UpdateQuestionPayload {
  subjectId: string;
  topicId: string;
  subTopicId?: string | null;
  learningOutcomeId?: string | null;
  questionType: QuestionType;
  difficulty: DifficultyLevel;
  sourceType: QuestionSourceType;
  sourceYear?: number | null;
  content: string;
  mediaUrl?: string | null;
  status: QuestionStatus;
  options: CreateQuestionOptionPayload[];
  questionStyleIds?: string[];
}

export interface TeacherOptionDto {
  id: string;
  optionLabel: string;
  content: string;
  isCorrect: boolean;
}

export interface ExamQuestionTeacherDto {
  id: string;
  examSectionId: string;
  questionId: string;
  questionNumber: number;
  order: number;
  content: string;
  mediaUrl: string | null;
  correctOption: string;
  options: TeacherOptionDto[];
}

export interface AddExamQuestionPayload {
  examSectionId: string;
  questionId: string;
  questionNumber: number;
  order: number;
}

export interface StudentQuestionResultDto {
  examQuestionId: string;
  questionId: string;
  questionNumber: number;
  content: string;
  selectedOption: string | null;
  correctOption: string | null;
  isCorrect: boolean | null;
  isBlank: boolean;
  netContribution: number;
}

export interface TaxonomyDto {
  id: string;
  code: string;
  name: string;
  parentId: string | null;
}
