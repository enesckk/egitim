export type MeetingStatus = 'upcoming' | 'completed' | 'cancelled';

export interface CoachMeetingItem {
  id: string;
  studentId: string;
  studentName: string;
  initials: string;
  grade: string;
  field: string;
  dateStr: string;
  timeStr: string;
  durationMinutes: number;
  type: string;
  locationType: 'Online Video' | 'Yüz Yüze • Kurum';
  status: MeetingStatus;
  notes?: string;
}

export interface CoachMeetingsViewModel {
  meetings: CoachMeetingItem[];
}
