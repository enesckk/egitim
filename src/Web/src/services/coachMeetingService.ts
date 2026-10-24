import { apiClient } from './api/apiClient';

export interface CoachMeetingDto {
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
  status: 'upcoming' | 'completed' | 'cancelled';
  notes?: string;
  startTime: string;
  endTime: string;
}

export interface CreateCoachMeetingPayload {
  studentId: string;
  startTime: string;
  durationMinutes: number;
  meetingType: string;
  locationType: string;
  notes?: string;
}

export interface UpdateCoachMeetingPayload {
  startTime?: string;
  durationMinutes?: number;
  meetingType?: string;
  locationType?: string;
  status?: string;
  notes?: string;
}

export const coachMeetingService = {
  listMeetings: async (): Promise<CoachMeetingDto[]> => {
    return apiClient.get<CoachMeetingDto[]>('/api/v1/coaching/meetings');
  },

  createMeeting: async (payload: CreateCoachMeetingPayload): Promise<CoachMeetingDto> => {
    return apiClient.post<CoachMeetingDto>('/api/v1/coaching/meetings', payload);
  },

  updateMeeting: async (meetingId: string, payload: UpdateCoachMeetingPayload): Promise<CoachMeetingDto> => {
    return apiClient.put<CoachMeetingDto>(`/api/v1/coaching/meetings/${meetingId}`, payload);
  },

  deleteMeeting: async (meetingId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/coaching/meetings/${meetingId}`);
  },
};
