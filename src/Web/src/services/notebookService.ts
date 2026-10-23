import { apiClient } from './api/apiClient';

export interface NotebookEntryDto {
  id: string;
  studentId: string;
  title: string;
  content: string;
  subjectId?: string | null;
  topicId?: string | null;
  tags?: string | null;
  isPinned: boolean;
  createdAt: string;
  updatedAt?: string | null;
}

export interface PaginatedList<T> {
  items: T[];
  pageIndex: number;
  totalPages: number;
  totalCount: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface ListNotebookEntriesParams {
  page?: number;
  pageSize?: number;
  subjectId?: string;
  searchTerm?: string;
  isPinned?: boolean;
  [key: string]: string | number | boolean | null | undefined;
}

export interface CreateNotebookEntryPayload {
  title: string;
  content: string;
  subjectId?: string | null;
  topicId?: string | null;
  tags?: string | null;
  isPinned?: boolean;
}

export interface UpdateNotebookEntryPayload {
  title: string;
  content: string;
  subjectId?: string | null;
  topicId?: string | null;
  tags?: string | null;
  isPinned?: boolean;
}

export const notebookService = {
  getEntries: async (params?: ListNotebookEntriesParams): Promise<PaginatedList<NotebookEntryDto>> => {
    return apiClient.get<PaginatedList<NotebookEntryDto>>('/api/v1/notebook', { params });
  },

  getEntryById: async (id: string): Promise<NotebookEntryDto> => {
    return apiClient.get<NotebookEntryDto>(`/api/v1/notebook/${id}`);
  },

  createEntry: async (payload: CreateNotebookEntryPayload): Promise<NotebookEntryDto> => {
    return apiClient.post<NotebookEntryDto>('/api/v1/notebook', payload);
  },

  updateEntry: async (id: string, payload: UpdateNotebookEntryPayload): Promise<NotebookEntryDto> => {
    return apiClient.put<NotebookEntryDto>(`/api/v1/notebook/${id}`, payload);
  },

  deleteEntry: async (id: string): Promise<void> => {
    await apiClient.delete(`/api/v1/notebook/${id}`);
  },
};
