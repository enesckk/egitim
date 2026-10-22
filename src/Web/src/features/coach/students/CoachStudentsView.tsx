import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Users, AlertCircle, RefreshCw } from 'lucide-react';
import { academicService, StudentSummaryDto } from '@/services/academicService';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { StudentDirectoryItem } from './components/StudentDirectoryItem';
import { AssignedStudentDetail, CoachStudentsViewModel, StudentStatus } from './types';

export interface CoachStudentsViewProps {
  initialData?: CoachStudentsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

function mapStudentSummary(
  dto: StudentSummaryDto,
  adherencePercentage: number,
  weeklyHours: number,
  hasActivePlan: boolean
): AssignedStudentDetail {
  const fullName = dto.fullName || `${dto.firstName || ''} ${dto.lastName || ''}`.trim() || 'Öğrenci';
  const initials =
    `${dto.firstName?.[0] || ''}${dto.lastName?.[0] || ''}`.toUpperCase() ||
    fullName
      .split(' ')
      .filter(Boolean)
      .map((p) => p[0])
      .join('')
      .toUpperCase()
      .slice(0, 2) ||
    'ÖĞ';

  let status: StudentStatus = 'takipte';
  if (hasActivePlan) {
    if (adherencePercentage < 40) status = 'kritik';
    else if (adherencePercentage < 70) status = 'dikkat';
  }

  return {
    id: dto.id,
    name: fullName,
    initials,
    grade: dto.gradeLevel ? `${dto.gradeLevel}. Sınıf` : '12. Sınıf',
    field: dto.schoolName || 'Genel Alan',
    exam: 'TYT / AYT',
    status,
    planAdherence: adherencePercentage,
    weeklyStudyHours: weeklyHours,
    lastActivity: dto.createdAt ? new Date(dto.createdAt).toLocaleDateString('tr-TR') : 'Aktif',
    privateNotes: [],
    weeklyPlanTasks: [],
    recentExams: [],
  };
}

export const CoachStudentsView: React.FC<CoachStudentsViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [data, setData] = useState<CoachStudentsViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [gradeFilter, setGradeFilter] = useState('');

  const fetchStudents = useCallback(async () => {
    if (initialData) return;

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await academicService.getStudents({ page: 1, pageSize: 50 });
      const items = res?.items || [];

      // Enrich up to 10 students with active study plan adherence (bounded to prevent N+1)
      const enriched = await Promise.all(
        items.map(async (item, index) => {
          if (index < 10) {
            try {
              const activePlan = await academicService.getActiveStudyPlan(item.id);
              if (activePlan?.plan) {
                const adherence = Math.round((activePlan.plan.taskProgressRate || 0) * 100);
                const hours = Math.round(((activePlan.plan.completedTaskMinutes || 0) / 60) * 10) / 10;
                return mapStudentSummary(item, adherence, hours, true);
              }
            } catch {
              // No active plan or unreachable
            }
          }
          return mapStudentSummary(item, 100, 0, false);
        })
      );

      setData({ students: enriched });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Atanmış öğrenci listesi alınamadı.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchStudents();
    }
  }, [initialData, fetchStudents]);

  const handleSelectStudent = (student: AssignedStudentDetail) => {
    navigate(`/coach/students/${student.id}`);
  };

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchStudents();
    }
  };

  const activeData = data || initialData || { students: [] };
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none" data-testid="coach-students-loading">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="space-y-3">
          <Skeleton className="h-20 w-full rounded-2xl" />
          <Skeleton className="h-20 w-full rounded-2xl" />
          <Skeleton className="h-20 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4" data-testid="coach-students-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenciler Yüklenemedi">
          {activeError}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const filteredStudents = activeData.students.filter((student) => {
    const matchesSearch =
      student.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      student.field.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesStatus = !statusFilter || student.status === statusFilter;
    const matchesGrade = !gradeFilter || student.grade.includes(gradeFilter);
    return matchesSearch && matchesStatus && matchesGrade;
  });

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="coach-students-view">
      {/* 1. Header & Title */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Atanmış Öğrenciler
          </h1>
          <p className="text-xs text-neutral-400 mt-0.5">
            Koçluk portföyünüzdeki toplam {activeData.students.length} öğrencinin güncel durumu
          </p>
        </div>
      </div>

      {/* 2. Search & Filter Bar */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-3 sm:p-4 flex flex-col sm:flex-row items-center gap-2.5 shadow-soft-sm">
        <div className="flex-1 w-full relative">
          <Input
            placeholder="Öğrenci adı veya alan ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
            className="text-xs min-h-[38px]"
          />
        </div>

        <div className="flex items-center gap-2 w-full sm:w-auto">
          <Select
            options={[
              { value: 'takipte', label: 'Takipte (İyi)' },
              { value: 'dikkat', label: 'Dikkat Gerektiren' },
              { value: 'kritik', label: 'Kritik Durum' },
            ]}
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            placeholder="Tüm Durumlar"
            className="text-xs min-h-[38px] py-1.5 pl-2.5 pr-7 flex-1 sm:w-40"
          />

          <Select
            options={[
              { value: '10', label: '10. Sınıf' },
              { value: '11', label: '11. Sınıf' },
              { value: '12', label: '12. Sınıf' },
            ]}
            value={gradeFilter}
            onChange={(e) => setGradeFilter(e.target.value)}
            placeholder="Tüm Sınıflar"
            className="text-xs min-h-[38px] py-1.5 pl-2.5 pr-7 flex-1 sm:w-36"
          />
        </div>
      </div>

      {/* 3. Students List */}
      {filteredStudents.length > 0 ? (
        <div className="space-y-2.5">
          {filteredStudents.map((student) => (
            <StudentDirectoryItem
              key={student.id}
              student={student}
              onClick={handleSelectStudent}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<Users className="h-6 w-6 text-neutral-400" />}
          title="Öğrenci Bulunamadı"
          description="Arama kriterlerinize veya seçilen filtrelere uygun öğrenci kaydı yok."
        />
      )}
    </div>
  );
};
