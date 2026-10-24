import { apiClient } from './api/apiClient';

export interface BranchReportItem {
  classroomId: string;
  branchName: string;
  studentCount: number;
  coachCount: number;
  averageAdherence: number;
  averageNet: number;
}

export interface AdherenceDistributionItem {
  range: string;
  studentCount: number;
  percentage: number;
  color: string;
}

export interface SubjectAverageItem {
  subjectName: string;
  averageNet: number;
  averageCorrect: number;
  averageWrong: number;
}

export interface ExamPerformanceRow {
  attemptId: string;
  examTitle: string;
  classroomName: string;
  studentName: string;
  totalNet: number;
  score: number;
  takenAt: string;
}

export interface InstitutionalReportDto {
  institutionName: string;
  totalStudents: number;
  totalExams: number;
  totalAttempts: number;
  overallAverageNet: number;
  overallAverageAdherence: number;
  branchComparisons: BranchReportItem[];
  adherenceDistribution: AdherenceDistributionItem[];
  subjectAverages: SubjectAverageItem[];
  examPerformances: ExamPerformanceRow[];
}

export interface ReportFilterParams {
  classroomId?: string;
  examId?: string;
  startDate?: string;
  endDate?: string;
}

export const reportingService = {
  getInstitutionalReport: (filters?: ReportFilterParams): Promise<InstitutionalReportDto> => {
    const params = new URLSearchParams();
    if (filters?.classroomId) params.set('classroomId', filters.classroomId);
    if (filters?.examId) params.set('examId', filters.examId);
    if (filters?.startDate) params.set('startDate', filters.startDate);
    if (filters?.endDate) params.set('endDate', filters.endDate);

    const queryString = params.toString();
    const endpoint = `/api/v1/reporting/institution${queryString ? `?${queryString}` : ''}`;
    return apiClient.get<InstitutionalReportDto>(endpoint);
  },

  getExportUrl: (format: 'xlsx' | 'csv', filters?: ReportFilterParams): string => {
    const params = new URLSearchParams();
    params.set('format', format);
    if (filters?.classroomId) params.set('classroomId', filters.classroomId);
    if (filters?.examId) params.set('examId', filters.examId);
    if (filters?.startDate) params.set('startDate', filters.startDate);
    if (filters?.endDate) params.set('endDate', filters.endDate);

    return `/api/v1/reporting/institution/export?${params.toString()}`;
  },
};
