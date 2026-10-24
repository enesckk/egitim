import { apiClient } from './api/apiClient';
import { ClassroomDto, ClassroomDetailDto } from './adminService';
import { Student360Dto } from './academicService';

export interface TeacherSubjectDetailDto {
  id: string;
  subjectId: string;
  subjectName: string;
}

export interface TeacherMeDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  title?: string | null;
  institutionId: string;
  subjects: TeacherSubjectDetailDto[];
  assignedClassroomCount: number;
  assignedStudentCount: number;
}

export const teacherService = {
  getMe: (): Promise<TeacherMeDto> => {
    return apiClient.get<TeacherMeDto>('/api/v1/teachers/me');
  },

  listClassrooms: (params?: {
    gradeLevel?: number;
    searchTerm?: string;
    page?: number;
    pageSize?: number;
  }): Promise<ClassroomDto[]> => {
    return apiClient.get<ClassroomDto[]>('/api/v1/classrooms', { params });
  },

  getClassroomDetail: (id: string): Promise<ClassroomDetailDto> => {
    return apiClient.get<ClassroomDetailDto>(`/api/v1/classrooms/${id}`);
  },

  getStudent360: (studentId: string): Promise<Student360Dto> => {
    return apiClient.get<Student360Dto>(`/api/v1/students/${studentId}/360`);
  },

  getDashboard: (): Promise<any> => {
    return apiClient.get('/api/v1/teachers/dashboard');
  },
};
