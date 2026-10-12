import { apiClient } from './api/apiClient';
import {
  ExamDto,
  ExamDetailDto,
  StudentExamResultSummaryDto,
  ExamResultDetailDto,
  StudentExamAnalysisDto,
  PaginatedList,
} from '@/types/exam';

export interface ListExamsParams {
  page?: number;
  pageSize?: number;
  examTypeId?: string;
  from?: string;
  to?: string;
  status?: number;
}

export interface ListStudentResultsParams {
  page?: number;
  pageSize?: number;
  examTypeId?: string;
  from?: string;
  to?: string;
  onlyFinalized?: boolean;
}

export interface StudentAnalysisParams {
  examTypeId?: string;
  window?: number;
}

export const examService = {
  getExams: (params?: ListExamsParams): Promise<PaginatedList<ExamDto>> => {
    return apiClient.get<PaginatedList<ExamDto>>('/api/v1/exams', { params: params as Record<string, string | number | boolean> });
  },

  getExamDetail: (examId: string): Promise<ExamDetailDto> => {
    return apiClient.get<ExamDetailDto>(`/api/v1/exams/${examId}`);
  },

  getStudentExamResults: (
    studentId: string,
    params?: ListStudentResultsParams
  ): Promise<PaginatedList<StudentExamResultSummaryDto>> => {
    return apiClient.get<PaginatedList<StudentExamResultSummaryDto>>(`/api/v1/students/${studentId}/exam-results`, {
      params: params as Record<string, string | number | boolean>,
    });
  },

  getStudentExamResultDetail: (studentId: string, attemptId: string): Promise<ExamResultDetailDto> => {
    return apiClient.get<ExamResultDetailDto>(`/api/v1/students/${studentId}/exam-results/${attemptId}`);
  },

  getStudentExamAnalysis: (studentId: string, params?: StudentAnalysisParams): Promise<StudentExamAnalysisDto> => {
    return apiClient.get<StudentExamAnalysisDto>(`/api/v1/students/${studentId}/exam-analysis`, {
      params: params as Record<string, string | number | boolean>,
    });
  },

  createExam: (data: unknown): Promise<string> => {
    return apiClient.post<string>('/api/v1/exams', data);
  },

  createAttempt: (examId: string, data: unknown): Promise<string> => {
    return apiClient.post<string>(`/api/v1/exams/${examId}/attempts`, data);
  },

  updateAttempt: (attemptId: string, data: unknown): Promise<void> => {
    return apiClient.put<void>(`/api/v1/exam-attempts/${attemptId}`, data);
  },

  finalizeAttempt: (attemptId: string): Promise<void> => {
    return apiClient.post<void>(`/api/v1/exam-attempts/${attemptId}/finalize`);
  },

  getAttemptQuestionResults: (attemptId: string): Promise<import('@/types/question').StudentQuestionResultDto[]> => {
    return apiClient.get<import('@/types/question').StudentQuestionResultDto[]>(`/api/v1/exam-attempts/${attemptId}/question-results`);
  },

  // Sprint 5E — Manual Result Entry
  createSingleManualResult: (
    examId: string,
    command: import('@/types/examResultImport').CreateSingleManualResultCommand
  ): Promise<import('@/types/examResultImport').SingleManualResultResponse> => {
    return apiClient.post<import('@/types/examResultImport').SingleManualResultResponse>(
      `/api/v1/exams/${examId}/manual-results`,
      command
    );
  },

  createBulkManualResults: (
    examId: string,
    command: import('@/types/examResultImport').CreateBulkManualResultCommand
  ): Promise<import('@/types/examResultImport').BulkManualResultResponse> => {
    return apiClient.post<import('@/types/examResultImport').BulkManualResultResponse>(
      `/api/v1/exams/${examId}/manual-results/bulk`,
      command
    );
  },

  // Sprint 5E — Excel Result Imports
  downloadAggregateTemplate: (examId: string): Promise<Blob> => {
    return apiClient.getBlob(`/api/v1/exams/${examId}/result-imports/templates/aggregate`);
  },

  downloadQuestionAnswersTemplate: (examId: string): Promise<Blob> => {
    return apiClient.getBlob(`/api/v1/exams/${examId}/result-imports/templates/question-answers`);
  },

  previewResultImport: (
    examId: string,
    importType: import('@/types/examResultImport').ImportType,
    file: File
  ): Promise<import('@/types/examResultImport').ExamResultImportPreviewDto> => {
    const formData = new FormData();
    formData.append('importType', String(importType));
    formData.append('file', file);
    return apiClient.postForm<import('@/types/examResultImport').ExamResultImportPreviewDto>(
      `/api/v1/exams/${examId}/result-imports/preview`,
      formData
    );
  },

  commitResultImport: (
    examId: string,
    importType: import('@/types/examResultImport').ImportType,
    fileHash: string,
    file: File
  ): Promise<import('@/types/examResultImport').ExamResultImportCommitResultDto> => {
    const formData = new FormData();
    formData.append('importType', String(importType));
    formData.append('fileHash', fileHash);
    formData.append('file', file);
    return apiClient.postForm<import('@/types/examResultImport').ExamResultImportCommitResultDto>(
      `/api/v1/exams/${examId}/result-imports/commit`,
      formData
    );
  },
};
