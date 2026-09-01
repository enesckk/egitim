import { AdminTeachersViewModel } from './types';

export const initialAdminTeachersData: AdminTeachersViewModel = {
  teachers: [
    {
      id: 'teacher-1',
      name: 'Kemal Bey',
      initials: 'KB',
      subject: 'Matematik & Fizik',
      assignedClasses: ['11-A Sayısal', '12-B Sayısal', '10-C Karma'],
      totalStudentCount: 64,
      branch: 'Kadıköy',
      status: 'active',
    },
    {
      id: 'teacher-2',
      name: 'Zehra Öğretmen',
      initials: 'ZÖ',
      subject: 'Türkçe & Edebiyat',
      assignedClasses: ['11-B TM', '12-A Sözel', '10-A'],
      totalStudentCount: 68,
      branch: 'Kadıköy',
      status: 'active',
    },
    {
      id: 'teacher-3',
      name: 'Ahmet Hoca',
      initials: 'AH',
      subject: 'Kimya & Biyoloji',
      assignedClasses: ['11-A Sayısal', '12-B Sayısal'],
      totalStudentCount: 40,
      branch: 'Şişli',
      status: 'active',
    },
    {
      id: 'teacher-4',
      name: 'Selin Öğretmen',
      initials: 'SÖ',
      subject: 'Tarih & Coğrafya',
      assignedClasses: ['11-B TM', '12-A Sözel'],
      totalStudentCount: 46,
      branch: 'Ümraniye',
      status: 'active',
    },
  ],
};
