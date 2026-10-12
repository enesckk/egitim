export enum ImportType {
  AggregateResults = 1,
  QuestionAnswers = 2,
}

export enum ImportIssueSeverity {
  Error = 1,
  Warning = 2,
}

export interface ManualSectionResultInput {
  examSectionId: string;
  correct: number;
  wrong: number;
  blank: number;
}

export interface CreateSingleManualResultCommand {
  studentId: string;
  sections: ManualSectionResultInput[];
}

export interface SingleManualResultResponse {
  attemptId: string;
  studentId: string;
  examId: string;
  attemptNumber: number;
  totalCorrect: number;
  totalWrong: number;
  totalBlank: number;
  totalNet: number;
  isRawAvailable: boolean;
  finalizedAt: string;
}

export interface BulkStudentManualResultItem {
  studentId: string;
  sections: ManualSectionResultInput[];
}

export interface CreateBulkManualResultCommand {
  studentResults: BulkStudentManualResultItem[];
}

export interface BulkManualResultResponse {
  examId: string;
  studentsProcessed: number;
  attemptsCreated: number;
  subjectResultsCreated: number;
  attemptIds: string[];
}

export interface ImportIssueDto {
  rowNumber: number;
  column: string;
  code: string;
  severity: string;
  message: string;
}

export interface MatchedStudentDto {
  id: string;
  studentNumber: string | null;
  fullName: string;
  matchMethod: string;
}

export interface ParsedAggregateSectionDto {
  examSectionId: string;
  subjectId: string;
  subjectName: string;
  correct: number;
  wrong: number;
  blank: number;
  calculatedNet: number;
}

export interface ParsedQuestionAnswerDto {
  examQuestionId: string;
  questionNumber: number;
  selectedOption: string;
  isCorrect: boolean;
  isBlank: boolean;
}

export interface ImportRowPreviewDto {
  rowNumber: number;
  studentIdentifier: string;
  matchedStudent: MatchedStudentDto | null;
  isValid: boolean;
  calculatedTotalNet: number | null;
  sections: ParsedAggregateSectionDto[];
  answers: ParsedQuestionAnswerDto[];
}

export interface ExamResultImportPreviewDto {
  fileHash: string;
  examId: string;
  importType: ImportType;
  templateVersion: number;
  totalRows: number;
  validRowsCount: number;
  invalidRowsCount: number;
  warningCount: number;
  canImport: boolean;
  issues: ImportIssueDto[];
  rows: ImportRowPreviewDto[];
}

export interface ExamResultImportCommitResultDto {
  importId: string;
  examId: string;
  fileHash: string;
  importedStudentsCount: number;
  createdAttemptsCount: number;
  importedAt: string;
}
