import { apiClient } from './api/apiClient';
import type { StudentSummaryDto } from './academicService';
export type { StudentSummaryDto } from './academicService';

export interface PaginatedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  institutionId?: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
  institutionId?: string;
}

export interface CreateStudentRequest {
  firstName: string;
  lastName: string;
  targetExam?: string;
  targetScore?: number | null;
  userId?: string | null;
  schoolName?: string | null;
  studentNumber?: string | null;
  gradeLevel?: number | null;
  email?: string;
  password?: string;
}

export interface StudentDto {
  id: string;
  firstName: string;
  lastName: string;
  targetExam?: string;
  targetScore?: number;
  userId?: string;
  schoolName?: string;
  studentNumber?: string;
  gradeLevel?: number;
  createdAt: string;
}

export interface TeacherListDto {
  id: string;
  firstName: string;
  lastName: string;
  title?: string | null;
}

export interface CreateTeacherRequest {
  institutionId: string;
  userId?: string;
  firstName: string;
  lastName: string;
  title?: string | null;
  email?: string;
  password?: string;
}

export interface CoachAssignedStudentDto {
  studentId: string;
  fullName: string;
  gradeLevel?: number | null;
  isPrimary: boolean;
}

export interface CoachSummaryDto {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  institutionId: string;
  assignedStudentCount: number;
  assignedStudents: CoachAssignedStudentDto[];
}

export interface CreateCoachRequest {
  institutionId: string;
  userId?: string;
  firstName: string;
  lastName: string;
  email?: string;
  password?: string;
}

export interface ClassroomTeacherDto {
  teacherId: string;
  fullName: string;
  title?: string | null;
  isPrimary: boolean;
}

export interface ClassroomStudentItemDto {
  studentId: string;
  fullName: string;
  studentNumber?: string | null;
  gradeLevel?: number | null;
}

export interface ClassroomDto {
  id: string;
  institutionId: string;
  name: string;
  branch?: string | null;
  gradeLevel?: number | null;
  academicYear?: string | null;
  studentCount: number;
  teacherCount: number;
  teachers: ClassroomTeacherDto[];
}

export interface ClassroomDetailDto {
  id: string;
  institutionId: string;
  name: string;
  branch?: string | null;
  gradeLevel?: number | null;
  academicYear?: string | null;
  teachers: ClassroomTeacherDto[];
  students: ClassroomStudentItemDto[];
}

export interface CreateClassroomRequest {
  institutionId?: string;
  name: string;
  branch?: string | null;
  gradeLevel?: number | null;
  academicYear?: string | null;
}

export interface ParentDto {
  id: string;
  userId: string;
  institutionId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  phoneNumber?: string | null;
  relationshipType?: string | null;
  linkedStudentsCount: number;
  createdAt: string;
}

export interface CreateParentRequest {
  userId: string;
  firstName: string;
  lastName: string;
  phoneNumber?: string;
  relationshipType?: string;
}

export const adminService = {
  // Users
  createUser: (cmd: CreateUserRequest): Promise<UserDto> => {
    return apiClient.post<UserDto>('/api/v1/users', cmd);
  },

  listUsers: (params?: {
    role?: string;
    searchTerm?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<UserDto>> => {
    return apiClient.get<PaginatedResult<UserDto>>('/api/v1/users', { params });
  },

  // Students
  createStudent: (cmd: CreateStudentRequest): Promise<StudentDto> => {
    return apiClient.post<StudentDto>('/api/v1/students', cmd);
  },

  listStudents: (params?: {
    page?: number;
    pageSize?: number;
    searchTerm?: string;
  }): Promise<PaginatedResult<StudentSummaryDto>> => {
    return apiClient.get<PaginatedResult<StudentSummaryDto>>('/api/v1/students', { params });
  },

  // Teachers
  createTeacher: (cmd: CreateTeacherRequest): Promise<TeacherListDto> => {
    return apiClient.post<TeacherListDto>('/api/v1/teachers', cmd);
  },

  listTeachers: (params?: {
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<TeacherListDto>> => {
    return apiClient.get<PaginatedResult<TeacherListDto>>('/api/v1/teachers', { params });
  },

  // Coaches
  createCoach: (cmd: CreateCoachRequest): Promise<CoachSummaryDto> => {
    return apiClient.post<CoachSummaryDto>('/api/v1/coaching/coaches', cmd);
  },

  listCoaches: (params?: {
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<CoachSummaryDto>> => {
    return apiClient.get<PaginatedResult<CoachSummaryDto>>('/api/v1/coaching/coaches', { params });
  },

  assignCoach: (studentId: string, coachId: string): Promise<void> => {
    return apiClient.post<void>('/api/v1/coaching/assign', { studentId, coachId });
  },

  // Classrooms
  createClassroom: (cmd: CreateClassroomRequest): Promise<ClassroomDto> => {
    return apiClient.post<ClassroomDto>('/api/v1/classrooms', cmd);
  },

  listClassrooms: (params?: {
    gradeLevel?: number;
    searchTerm?: string;
    page?: number;
    pageSize?: number;
  }): Promise<ClassroomDto[]> => {
    return apiClient.get<ClassroomDto[]>('/api/v1/classrooms', {
      params: {
        pageSize: 200,
        ...params,
      },
    });
  },

  getClassroom: (id: string): Promise<ClassroomDetailDto> => {
    return apiClient.get<ClassroomDetailDto>(`/api/v1/classrooms/${id}`);
  },

  assignTeacherToClassroom: (
    classroomId: string,
    teacherId: string,
    isPrimary: boolean = false
  ): Promise<ClassroomTeacherDto> => {
    return apiClient.post<ClassroomTeacherDto>(`/api/v1/classrooms/${classroomId}/teachers`, {
      classroomId,
      teacherId,
      isPrimary,
    });
  },

  removeTeacherFromClassroom: (classroomId: string, teacherId: string): Promise<void> => {
    return apiClient.delete<void>(`/api/v1/classrooms/${classroomId}/teachers/${teacherId}`);
  },

  assignStudentToClassroom: (
    classroomId: string,
    studentId: string
  ): Promise<ClassroomStudentItemDto> => {
    return apiClient.post<ClassroomStudentItemDto>(`/api/v1/classrooms/${classroomId}/students`, {
      classroomId,
      studentId,
    });
  },

  removeStudentFromClassroom: (classroomId: string, studentId: string): Promise<void> => {
    return apiClient.delete<void>(`/api/v1/classrooms/${classroomId}/students/${studentId}`);
  },

  // Parents
  listParents: (params?: {
    search?: string;
    page?: number;
    pageSize?: number;
  }): Promise<PaginatedResult<ParentDto>> => {
    return apiClient.get<PaginatedResult<ParentDto>>('/api/v1/parents', { params });
  },

  createParent: (cmd: CreateParentRequest): Promise<ParentDto> => {
    return apiClient.post<ParentDto>('/api/v1/parents', cmd);
  },

  linkParent: (studentId: string, parentId: string): Promise<void> => {
    return apiClient.post<void>(`/api/v1/students/${studentId}/parents`, { parentId });
  },

  // Institution Overview (Real Dashboard)
  getInstitutionOverview: (): Promise<any> => {
    return apiClient.get('/api/v1/institution/overview');
  },
};
