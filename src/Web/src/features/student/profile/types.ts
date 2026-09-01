export interface StudentProfileData {
  id: string;
  name: string;
  initials: string;
  subtitle: string; // '11. Sınıf • Sayısal'
  email: string;
  joined: string; // 'Eylül 2023'
  notificationPreferences: {
    emailNotifications: boolean;
    smsNotifications: boolean;
    studyReminders: boolean;
    examResultsAlerts: boolean;
  };
}
