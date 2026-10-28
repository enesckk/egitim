import { AdminTeachersViewModel } from './types';

export const initialAdminTeachersData: AdminTeachersViewModel = {
  teachers: [
    {
      id: 'teacher-1',
      name: 'Kemal Yılmaz',
      firstName: 'Kemal',
      lastName: 'Yılmaz',
      initials: 'KY',
      title: 'Matematik Öğretmeni',
      assignedClassrooms: [{ id: 'cls-1', name: '12-A Sayısal', isPrimary: true, studentCount: 22 }],
      assignedSubjectIds: ['sub-1'],
      totalStudentCount: 22,
      status: 'active',
    },
    {
      id: 'teacher-2',
      name: 'Zehra Kaya',
      firstName: 'Zehra',
      lastName: 'Kaya',
      initials: 'ZK',
      title: 'Fizik Öğretmeni',
      assignedClassrooms: [{ id: 'cls-2', name: '11-B Sayısal', isPrimary: false, studentCount: 18 }],
      assignedSubjectIds: ['sub-2'],
      totalStudentCount: 18,
      status: 'active',
    },
  ],
};
