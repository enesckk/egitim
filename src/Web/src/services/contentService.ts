import { apiClient } from './api/apiClient';

export interface ContentMaterialAssignmentDto {
  id: string;
  classroomId: string;
  classroomName?: string;
  note?: string;
  isActive: boolean;
  createdAt: string;
}

export interface TeacherMaterialDto {
  id: string;
  title: string;
  subject: string;
  topic: string;
  grade: string;
  materialType: string;
  description?: string;
  pageCount?: number;
  questionCount?: number;
  durationMinutes?: number;
  originalFileName: string;
  fileSizeBytes: number;
  assignedClassesCount: number;
  createdAt: string;
  assignments: ContentMaterialAssignmentDto[];
}

export interface StudentMaterialDto {
  id: string;
  title: string;
  subject: string;
  topic: string;
  grade: string;
  materialType: string;
  description?: string;
  pageCount?: number;
  questionCount?: number;
  durationMinutes?: number;
  originalFileName: string;
  fileSizeBytes: number;
  classroomName: string;
  teacherNote?: string;
  assignedAt: string;
}

export const contentService = {
  getTeacherMaterials: async (): Promise<TeacherMaterialDto[]> => {
    return apiClient.get<TeacherMaterialDto[]>('/api/content/materials/teacher');
  },

  createMaterial: async (formData: FormData): Promise<TeacherMaterialDto> => {
    return apiClient.postForm<TeacherMaterialDto>('/api/content/materials', formData);
  },

  assignToClassroom: async (materialId: string, classroomId: string, note?: string): Promise<{ success: boolean }> => {
    return apiClient.post<{ success: boolean }>(`/api/content/materials/${materialId}/assign`, {
      classroomId,
      note,
    });
  },

  revokeAssignment: async (materialId: string, classroomId: string): Promise<{ success: boolean }> => {
    return apiClient.post<{ success: boolean }>(`/api/content/materials/${materialId}/revoke/${classroomId}`, {});
  },

  getStudentMaterials: async (): Promise<StudentMaterialDto[]> => {
    return apiClient.get<StudentMaterialDto[]>('/api/content/materials/student');
  },

  downloadMaterialUrl: (materialId: string): string => {
    return `/api/content/materials/${materialId}/download`;
  },
};
