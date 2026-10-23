import { apiClient } from './api/apiClient';

export interface CoachPrivateNoteDto {
  id: string;
  studentId: string;
  coachId: string;
  category: 'Akademik' | 'Motivasyon' | 'Veli Görüşmesi' | 'Genel';
  content: string;
  createdAt: string;
  updatedAt?: string | null;
}

export interface CreateCoachPrivateNotePayload {
  category: 'Akademik' | 'Motivasyon' | 'Veli Görüşmesi' | 'Genel';
  content: string;
}

export const coachNoteService = {
  getPrivateNotes: async (studentId: string): Promise<CoachPrivateNoteDto[]> => {
    return apiClient.get<CoachPrivateNoteDto[]>(`/api/v1/coaching/students/${studentId}/notes`);
  },

  createPrivateNote: async (
    studentId: string,
    payload: CreateCoachPrivateNotePayload
  ): Promise<CoachPrivateNoteDto> => {
    return apiClient.post<CoachPrivateNoteDto>(`/api/v1/coaching/students/${studentId}/notes`, payload);
  },

  deletePrivateNote: async (noteId: string): Promise<void> => {
    await apiClient.delete(`/api/v1/coaching/notes/${noteId}`);
  },
};
