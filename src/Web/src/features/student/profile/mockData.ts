import { StudentProfileData } from './types';

export const initialStudentProfileData: StudentProfileData = {
  id: 'student-01',
  name: 'Ayşe Kaya',
  initials: 'AK',
  subtitle: '11. Sınıf • Sayısal',
  email: 'ayse.kaya@ogrenci.bilimakademi.com',
  joined: 'Eylül 2023',
  notificationPreferences: {
    emailNotifications: true,
    smsNotifications: false,
    studyReminders: true,
    examResultsAlerts: true,
  },
};
