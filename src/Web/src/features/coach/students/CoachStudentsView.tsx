import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Users, AlertCircle, RefreshCw, Calendar, MessageSquare } from 'lucide-react';
import { academicService, StudentSummaryDto } from '@/services/academicService';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { useAuth } from '@/auth';
import { StudentDirectoryItem } from './components/StudentDirectoryItem';
import { AssignedStudentDetail, CoachStudentsViewModel, StudentComplianceStatus } from './types';

export interface CoachStudentsViewProps {
  initialData?: CoachStudentsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

function mapStudentSummary(
  dto: StudentSummaryDto,
  adherencePercentage?: number,
  weeklyHours?: number,
  hasActivePlan: boolean = false
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

  let status: StudentComplianceStatus = 'plansiz';
  if (hasActivePlan && typeof adherencePercentage === 'number') {
    if (adherencePercentage < 40) status = 'kritik';
    else if (adherencePercentage < 70) status = 'dikkat';
    else status = 'takipte';
  }

  return {
    id: dto.id,
    name: fullName,
    initials,
    grade: dto.gradeLevel ? `${dto.gradeLevel}. Sınıf` : '12. Sınıf',
    field: dto.schoolName || 'Kayıtlı Öğrenci',
    schoolName: dto.schoolName || undefined,
    exam: 'TYT / AYT',
    status,
    hasActivePlan,
    planAdherence: adherencePercentage,
    weeklyStudyHours: weeklyHours,
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
  const { user } = useAuth();

  const [data, setData] = useState<CoachStudentsViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchQuery, setSearchQuery] = useState('');
  const [planFilter, setPlanFilter] = useState('');
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
          return mapStudentSummary(item, undefined, undefined, false);
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

  const handleNavigateTab = (studentId: string, tab: 'plan' | 'tasks' | 'notes' | 'overview') => {
    navigate(`/coach/students/${studentId}?tab=${tab}`);
  };

  const handleSendMessage = (studentId: string) => {
    navigate(`/coach/messages?studentId=${studentId}`);
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

  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="coach-students-loading">
        <Skeleton className="h-24 w-full rounded-3xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="space-y-3">
          <Skeleton className="h-20 w-full rounded-2xl" />
          <Skeleton className="h-20 w-full rounded-2xl" />
          <Skeleton className="h-20 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 select-none p-4" data-testid="coach-students-error">
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
    const q = searchQuery.toLowerCase().trim();
    const matchesSearch =
      !q ||
      student.name.toLowerCase().includes(q) ||
      student.field.toLowerCase().includes(q) ||
      (student.schoolName && student.schoolName.toLowerCase().includes(q));

    let matchesPlan = true;
    if (planFilter === 'active') {
      matchesPlan = Boolean(student.hasActivePlan);
    } else if (planFilter === 'no_plan') {
      matchesPlan = !student.hasActivePlan;
    } else if (planFilter === 'attention') {
      matchesPlan = student.status === 'dikkat' || student.status === 'kritik';
    }

    const matchesGrade = !gradeFilter || student.grade.includes(gradeFilter);
    return matchesSearch && matchesPlan && matchesGrade;
  });

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="coach-students-view">
      {/* 1. Header Banner */}
      <div className="bg-[#14233E] rounded-3xl p-5 sm:p-7 text-white shadow-soft-sm relative overflow-hidden border border-navy-800">
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1.5">
              <span className="text-xs px-2.5 py-0.5 rounded-full bg-navy-800/80 text-blue-300 border border-navy-700 font-medium">
                {user?.institutionName || 'Kurum Portalı'}
              </span>
              <span className="text-navy-500">•</span>
              <span className="text-xs font-mono text-navy-300">
                {activeData.students.length} Kayıtlı Öğrenci
              </span>
            </div>
            <h1 className="font-serif text-2xl sm:text-3xl font-bold tracking-tight text-white leading-tight">
              Atanmış Öğrencilerim
            </h1>
            <p className="text-xs sm:text-sm text-navy-200 mt-1 max-w-xl leading-relaxed">
              Kurumunuz tarafından size aktif olarak atanmış öğrencilerin güncel çalışma planları ve performans takibi.
            </p>
          </div>

          <div className="flex items-center gap-2.5 shrink-0 flex-wrap">
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/coach/meetings')}
              leftIcon={<Calendar className="h-4 w-4" />}
              className="bg-white/10 hover:bg-white/15 text-white border-white/20 text-xs rounded-xl"
            >
              Görüşmeler
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={() => navigate('/coach/messages')}
              leftIcon={<MessageSquare className="h-4 w-4" />}
              className="bg-[#315FE8] hover:bg-blue-600 text-white text-xs rounded-xl shadow-soft-xs"
            >
              Mesajlar
            </Button>
          </div>
        </div>
      </div>

      {/* 2. Responsive Search & Filter Bar */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-3.5 sm:p-4 flex flex-col sm:flex-row items-center gap-3 shadow-soft-xs">
        <div className="flex-1 w-full relative">
          <Input
            placeholder="Öğrenci adı veya okul ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
            className="text-xs min-h-[40px] bg-neutral-50/50 border-neutral-200"
          />
        </div>

        <div className="flex items-center gap-2.5 w-full sm:w-auto">
          <Select
            placeholder="Tüm Plan Durumları"
            options={[
              { value: 'active', label: 'Aktif Planı Olanlar' },
              { value: 'attention', label: 'Takip Gerektirenler' },
              { value: 'no_plan', label: 'Aktif Planı Olmayanlar' },
            ]}
            value={planFilter}
            onChange={(e) => setPlanFilter(e.target.value)}
            className="text-xs min-h-[40px] py-1.5 pl-2.5 pr-7 flex-1 sm:w-48 bg-neutral-50/50 border-neutral-200"
          />

          <Select
            placeholder="Tüm Sınıflar"
            options={[
              { value: '11', label: '11. Sınıf' },
              { value: '12', label: '12. Sınıf' },
            ]}
            value={gradeFilter}
            onChange={(e) => setGradeFilter(e.target.value)}
            className="text-xs min-h-[40px] py-1.5 pl-2.5 pr-7 flex-1 sm:w-36 bg-neutral-50/50 border-neutral-200"
          />
        </div>
      </div>

      {/* 3. Students List */}
      {filteredStudents.length > 0 ? (
        <div className="space-y-3">
          {filteredStudents.map((student) => (
            <StudentDirectoryItem
              key={student.id}
              student={student}
              onClick={handleSelectStudent}
              onNavigateTab={handleNavigateTab}
              onSendMessage={handleSendMessage}
            />
          ))}
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 sm:p-12 text-center shadow-soft-xs">
          <EmptyState
            icon={<Users className="h-8 w-8 text-neutral-400" />}
            title="Öğrenci Bulunamadı"
            description="Arama kriterlerinize veya seçilen filtrelere uygun öğrenci kaydı bulunamadı."
          />
        </div>
      )}
    </div>
  );
};

export default CoachStudentsView;
