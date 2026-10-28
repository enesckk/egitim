import { AdminClassesViewModel } from './types';

export const initialAdminClassesData: AdminClassesViewModel = {
  classes: [
    {
      id: 'class-1',
      name: '12-A',
      grade: '12. Sınıf',
      gradeLevel: 12,
      branch: 'Sayısal',
      field: 'Sayısal',
      academicYear: '2026-2027',
      studentCount: 24,
      teacherCount: 2,
      teachers: [
        {
          teacherId: 't-1',
          fullName: 'Ayşe Kaya',
          title: 'Matematik Öğretmeni',
          isPrimary: true,
        },
      ],
      assignedTeachers: ['Ayşe Kaya (Matematik)'],
    },
    {
      id: 'class-2',
      name: '11-B',
      grade: '11. Sınıf',
      gradeLevel: 11,
      branch: 'Eşit Ağırlık',
      field: 'Eşit Ağırlık',
      academicYear: '2026-2027',
      studentCount: 20,
      teacherCount: 1,
      teachers: [
        {
          teacherId: 't-2',
          fullName: 'Mehmet Yılmaz',
          title: 'Türkçe Öğretmeni',
          isPrimary: true,
        },
      ],
      assignedTeachers: ['Mehmet Yılmaz (Türkçe)'],
    },
  ],
};
