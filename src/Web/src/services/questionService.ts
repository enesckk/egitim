import { apiClient } from './api/apiClient';
import {
  QuestionDto,
  QuestionDetailDto,
  QuestionStyleDto,
  GetQuestionsParams,
  CreateQuestionPayload,
  UpdateQuestionPayload,
  ExamQuestionTeacherDto,
  AddExamQuestionPayload,
} from '@/types/question';
import { PaginatedList } from '@/types/exam';

export const questionService = {
  getQuestionStyles: (): Promise<QuestionStyleDto[]> => {
    return apiClient.get<QuestionStyleDto[]>('/api/v1/question-styles');
  },

  getQuestions: (params?: GetQuestionsParams): Promise<PaginatedList<QuestionDto>> => {
    return apiClient.get<PaginatedList<QuestionDto>>('/api/v1/questions', {
      params: params as Record<string, string | number | boolean | undefined | null>,
    });
  },

  getQuestion: (id: string): Promise<QuestionDetailDto> => {
    return apiClient.get<QuestionDetailDto>(`/api/v1/questions/${id}`);
  },

  createQuestion: (payload: CreateQuestionPayload): Promise<string> => {
    return apiClient.post<string>('/api/v1/questions', payload);
  },

  updateQuestion: (id: string, payload: UpdateQuestionPayload): Promise<void> => {
    return apiClient.put<void>(`/api/v1/questions/${id}`, payload);
  },

  archiveQuestion: (id: string): Promise<void> => {
    return apiClient.post<void>(`/api/v1/questions/${id}/archive`);
  },

  getExamQuestions: (examId: string): Promise<ExamQuestionTeacherDto[]> => {
    return apiClient.get<ExamQuestionTeacherDto[]>(`/api/v1/exams/${examId}/questions`);
  },

  addExamQuestion: (examId: string, payload: AddExamQuestionPayload): Promise<string> => {
    return apiClient.post<string>(`/api/v1/exams/${examId}/questions`, payload);
  },
};
