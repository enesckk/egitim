import { apiClient } from './api/apiClient';
import { ApiError } from './api/apiErrors';
import { TaxonomyDto } from '@/types/question';
import {
  StudentSubjectPerformanceDto,
  StudentTopicPerformanceDto,
  StudentLearningOutcomePerformanceDto,
  AcademicCoverageSummaryDto,
  SubjectTrendDto,
  TopicTrendDto,
  LearningOutcomeTrendDto,
  SubjectClassificationDto,
  TopicClassificationDto,
  LearningOutcomeClassificationDto,
  StudentKnowledgeMapDto,
  AcademicRecommendationDto,
  AcademicStudyPlanDto,
  PersistentStudyPlanDto,
  PersistentStudyTaskDto,
  PersistentStudyPlanDetailDto,
  AcademicAiSummaryDto,
  StudentQuestionStyleAnalyticsDto,
  QuestionStyleTrendDto,
} from '@/features/student/academic/types';

export interface AcademicQueryParams extends Record<string, string | number | boolean | null | undefined> {
  examTypeId?: string;
  windowSize?: number;
  limit?: number;
  recommendationLimit?: number;
  planDays?: number;
  dailyStudyMinutes?: number;
  startDate?: string;
  from?: string;
  to?: string;
}

export interface StudentSummaryDto {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  status: string;
  institutionId: string;
  createdAt: string;
  schoolName?: string | null;
  studentNumber?: string | null;
  gradeLevel?: number | null;
  enrollmentDate?: string | null;
  userId?: string | null;
}

export interface StudentGoalSummaryDto {
  id: string;
  title: string;
  targetExamTypeId?: string | null;
  targetScore?: number | null;
  targetRank?: number | null;
  targetSchoolName?: string | null;
  effectiveDate: string;
}

export interface StudentCoachInfoDto {
  coachId: string;
  name: string;
  isPrimary: boolean;
  assignedAt: string;
}

export interface StudentParentInfoDto {
  parentId: string;
  name: string;
  relationshipType: string;
}

export interface AcademicRefDto {
  id: string;
  code: string;
  name: string;
}

export interface RecentExamInfoDto {
  latestExamId: string;
  title: string;
  examType: string;
  takenAt: string;
  totalNet: number;
  previousNet: number;
  delta: number;
}

export interface Student360Dto {
  student: StudentSummaryDto;
  currentGoals: StudentGoalSummaryDto[];
  coaches: StudentCoachInfoDto[];
  parents: StudentParentInfoDto[];
  examTypes: AcademicRefDto[];
  recentExam?: RecentExamInfoDto | null;
}

export const academicService = {
  getCurrentStudentId: async (signal?: AbortSignal): Promise<string | null> => {
    try {
      const res = await apiClient.get<{ items: Array<{ id: string }> }>('/api/v1/students', {
        params: { pageSize: 1 },
        signal,
      });
      if (res && res.items && res.items.length > 0) {
        return res.items[0].id;
      }
      return null;
    } catch {
      return null;
    }
  },

  getStudents: (
    params?: { page?: number; pageSize?: number; searchTerm?: string },
    signal?: AbortSignal
  ): Promise<{ items: StudentSummaryDto[]; totalCount: number; page: number; pageSize: number }> => {
    return apiClient.get<{ items: StudentSummaryDto[]; totalCount: number; page: number; pageSize: number }>(
      '/api/v1/students',
      { params, signal }
    );
  },

  getStudent: (studentId: string, signal?: AbortSignal): Promise<StudentSummaryDto> => {
    return apiClient.get<StudentSummaryDto>(`/api/v1/students/${studentId}`, { signal });
  },

  getStudent360: (studentId: string, signal?: AbortSignal): Promise<Student360Dto> => {
    return apiClient.get<Student360Dto>(`/api/v1/students/${studentId}/360`, { signal });
  },

  getTaxonomy: (level: string, parentId?: string, signal?: AbortSignal): Promise<TaxonomyDto[]> => {
    return apiClient.get<TaxonomyDto[]>(`/api/v1/academic/${level}`, {
      params: parentId ? { parentId } : undefined,
      signal,
    });
  },

  getSubjectPerformance: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<StudentSubjectPerformanceDto[]> => {
    return apiClient.get<StudentSubjectPerformanceDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/subjects`,
      { params, signal }
    );
  },

  getTopicPerformance: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<StudentTopicPerformanceDto[]> => {
    return apiClient.get<StudentTopicPerformanceDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/topics`,
      { params, signal }
    );
  },

  getLearningOutcomePerformance: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<StudentLearningOutcomePerformanceDto[]> => {
    return apiClient.get<StudentLearningOutcomePerformanceDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/learning-outcomes`,
      { params, signal }
    );
  },

  getCoverageSummary: (studentId: string, signal?: AbortSignal): Promise<AcademicCoverageSummaryDto> => {
    return apiClient.get<AcademicCoverageSummaryDto>(
      `/api/v1/students/${studentId}/academic-intelligence/coverage`,
      { signal }
    );
  },

  getSubjectTrends: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<SubjectTrendDto[]> => {
    return apiClient.get<SubjectTrendDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/trends/subjects`,
      { params, signal }
    );
  },

  getTopicTrends: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<TopicTrendDto[]> => {
    return apiClient.get<TopicTrendDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/trends/topics`,
      { params, signal }
    );
  },

  getLearningOutcomeTrends: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<LearningOutcomeTrendDto[]> => {
    return apiClient.get<LearningOutcomeTrendDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/trends/learning-outcomes`,
      { params, signal }
    );
  },

  getSubjectClassifications: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<SubjectClassificationDto[]> => {
    return apiClient.get<SubjectClassificationDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/classifications/subjects`,
      { params, signal }
    );
  },

  getTopicClassifications: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<TopicClassificationDto[]> => {
    return apiClient.get<TopicClassificationDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/classifications/topics`,
      { params, signal }
    );
  },

  getLearningOutcomeClassifications: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<LearningOutcomeClassificationDto[]> => {
    return apiClient.get<LearningOutcomeClassificationDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/classifications/learning-outcomes`,
      { params, signal }
    );
  },

  getKnowledgeMap: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<StudentKnowledgeMapDto> => {
    return apiClient.get<StudentKnowledgeMapDto>(
      `/api/v1/students/${studentId}/academic-intelligence/knowledge-map`,
      { params, signal }
    );
  },

  getRecommendations: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<AcademicRecommendationDto[]> => {
    return apiClient.get<AcademicRecommendationDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/recommendations`,
      { params, signal }
    );
  },

  getStudyPlan: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<AcademicStudyPlanDto> => {
    return apiClient.get<AcademicStudyPlanDto>(
      `/api/v1/students/${studentId}/academic-intelligence/study-plan`,
      { params, signal }
    );
  },

  acceptStudyPlan: (
    studentId: string,
    params?: AcademicQueryParams
  ): Promise<PersistentStudyPlanDetailDto> => {
    return apiClient.post<PersistentStudyPlanDetailDto>(
      `/api/v1/students/${studentId}/study-plans/from-academic-intelligence`,
      params || {}
    );
  },

  getActiveStudyPlan: async (
    studentId: string,
    signal?: AbortSignal
  ): Promise<PersistentStudyPlanDetailDto | null> => {
    try {
      return await apiClient.get<PersistentStudyPlanDetailDto>(
        `/api/v1/students/${studentId}/study-plans/active`,
        { signal }
      );
    } catch (err: unknown) {
      if (
        (err instanceof ApiError && err.status === 404) ||
        (typeof err === 'object' && err !== null && (err as { status?: number }).status === 404)
      ) {
        return null;
      }
      throw err;
    }
  },

  getStudyPlanDetail: (
    studentId: string,
    planId: string,
    signal?: AbortSignal
  ): Promise<PersistentStudyPlanDetailDto> => {
    return apiClient.get<PersistentStudyPlanDetailDto>(
      `/api/v1/students/${studentId}/study-plans/${planId}`,
      { signal }
    );
  },

  listStudyPlans: (
    studentId: string,
    signal?: AbortSignal
  ): Promise<PersistentStudyPlanDto[]> => {
    return apiClient.get<PersistentStudyPlanDto[]>(
      `/api/v1/students/${studentId}/study-plans`,
      { signal }
    );
  },

  updateTaskCompletion: (
    studentId: string,
    planId: string,
    taskId: string,
    isCompleted: boolean
  ): Promise<PersistentStudyTaskDto> => {
    return apiClient.patch<PersistentStudyTaskDto>(
      `/api/v1/students/${studentId}/study-plans/${planId}/tasks/${taskId}/completion`,
      { isCompleted }
    );
  },

  cancelStudyPlan: (
    studentId: string,
    planId: string
  ): Promise<void> => {
    return apiClient.post<void>(
      `/api/v1/students/${studentId}/study-plans/${planId}/cancel`,
      {}
    );
  },

  getAiSummary: (
    studentId: string,
    audience: string = 'student',
    signal?: AbortSignal
  ): Promise<AcademicAiSummaryDto> => {
    return apiClient.get<AcademicAiSummaryDto>(
      `/api/v1/students/${studentId}/academic-intelligence/ai-summary`,
      { params: { audience }, signal }
    );
  },

  getQuestionStyleAnalytics: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<StudentQuestionStyleAnalyticsDto[]> => {
    return apiClient.get<StudentQuestionStyleAnalyticsDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/question-styles`,
      { params, signal }
    );
  },

  getQuestionStyleTrends: (
    studentId: string,
    params?: AcademicQueryParams,
    signal?: AbortSignal
  ): Promise<QuestionStyleTrendDto[]> => {
    return apiClient.get<QuestionStyleTrendDto[]>(
      `/api/v1/students/${studentId}/academic-intelligence/question-style-trends`,
      { params, signal }
    );
  },
};
