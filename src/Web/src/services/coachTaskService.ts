import { apiClient } from './api/apiClient';

export enum CoachTaskStatus {
  Active = 0,
  Completed = 1,
  Cancelled = 2,
}

export interface CoachTaskDto {
  id: string;
  institutionId: string;
  studentId: string;
  coachUserId: string;
  title: string;
  description?: string | null;
  dueDate?: string | null;
  durationMinutes?: number | null;
  subjectId?: string | null;
  subjectName?: string | null;
  topicId?: string | null;
  topicName?: string | null;
  learningOutcomeId?: string | null;
  learningOutcomeName?: string | null;
  status: CoachTaskStatus;
  isCompleted: boolean;
  createdAt: string;
  completedAt?: string | null;
  updatedAt?: string | null;
}

export interface CreateCoachTaskPayload {
  title: string;
  description?: string;
  dueDate?: string;
  durationMinutes?: number;
  subjectId?: string;
  topicId?: string;
  learningOutcomeId?: string;
}

export interface UpdateCoachTaskPayload {
  title: string;
  description?: string;
  dueDate?: string;
  durationMinutes?: number;
  subjectId?: string;
  topicId?: string;
  learningOutcomeId?: string;
}

export const coachTaskService = {
  getCoachTasks(studentId: string, status?: CoachTaskStatus): Promise<CoachTaskDto[]> {
    const params = status !== undefined ? { status } : undefined;
    return apiClient.get<CoachTaskDto[]>(`/api/v1/students/${studentId}/coach-tasks`, { params });
  },

  createCoachTask(studentId: string, payload: CreateCoachTaskPayload): Promise<CoachTaskDto> {
    return apiClient.post<CoachTaskDto>(`/api/v1/students/${studentId}/coach-tasks`, payload);
  },

  updateCoachTask(studentId: string, taskId: string, payload: UpdateCoachTaskPayload): Promise<CoachTaskDto> {
    return apiClient.put<CoachTaskDto>(`/api/v1/students/${studentId}/coach-tasks/${taskId}`, payload);
  },

  cancelCoachTask(studentId: string, taskId: string): Promise<CoachTaskDto> {
    return apiClient.post<CoachTaskDto>(`/api/v1/students/${studentId}/coach-tasks/${taskId}/cancel`);
  },

  toggleTaskCompletion(studentId: string, taskId: string, isCompleted: boolean): Promise<CoachTaskDto> {
    return apiClient.patch<CoachTaskDto>(`/api/v1/students/${studentId}/coach-tasks/${taskId}/completion`, { isCompleted });
  },
};
