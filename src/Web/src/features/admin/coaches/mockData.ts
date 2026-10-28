import { AdminCoachesViewModel } from './types';

export const initialAdminCoachesData: AdminCoachesViewModel = {
  coaches: [
    {
      id: 'coach-1',
      name: 'Hasan Yılmaz',
      firstName: 'Hasan',
      lastName: 'Yılmaz',
      initials: 'HY',
      institutionId: 'inst-1',
      assignedStudentCount: 3,
      assignedStudents: [
        { studentId: 's-1', fullName: 'Ali Can', gradeLevel: 12, isPrimary: true },
        { studentId: 's-2', fullName: 'Ayşe Kaya', gradeLevel: 11, isPrimary: true },
        { studentId: 's-3', fullName: 'Burak Demir', gradeLevel: 12, isPrimary: false },
      ],
      status: 'active',
    },
    {
      id: 'coach-2',
      name: 'Merve Aksoy',
      firstName: 'Merve',
      lastName: 'Aksoy',
      initials: 'MA',
      institutionId: 'inst-1',
      assignedStudentCount: 2,
      assignedStudents: [
        { studentId: 's-4', fullName: 'Canan Yıldız', gradeLevel: 10, isPrimary: true },
        { studentId: 's-5', fullName: 'Deniz Şen', gradeLevel: 12, isPrimary: true },
      ],
      status: 'active',
    },
  ],
};
