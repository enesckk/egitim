export interface StudentProfileData {
  id: string;
  name: string;
  initials: string;
  subtitle: string; // '11. Sınıf • Sayısal'
  email: string;
  joined?: string; // e.g. 'Eylül 2023' (omitted when not in backend contract)
  notificationPreferences?: {
    emailNotifications: boolean;
    smsNotifications: boolean;
    studyReminders: boolean;
    examResultsAlerts: boolean;
  };
}
