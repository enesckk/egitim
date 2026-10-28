import { AdminStudentDirectoryViewModel } from './types';

export const initialAdminStudentsData: AdminStudentDirectoryViewModel = {
  students: [
    {
      id: 'student-1',
      name: 'Ahmet Yılmaz',
      initials: 'AY',
      grade: '12. Sınıf',
      gradeLevel: 12,
      exam: 'YKS',
      schoolName: 'Demo Anadolu Lisesi',
      studentNumber: '1201',
      status: 'Active',
      createdAt: '2026-04-23T12:00:00Z',
    },
    {
      id: 'student-2',
      name: 'Zeynep Demir',
      initials: 'ZD',
      grade: '12. Sınıf',
      gradeLevel: 12,
      exam: 'YKS',
      schoolName: 'Demo Anadolu Lisesi',
      studentNumber: '1202',
      status: 'Active',
      createdAt: '2026-04-23T12:00:00Z',
    },
  ],
  totalCount: 2,
  page: 1,
  pageSize: 20,
  totalPages: 1,
};
