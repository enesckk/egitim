import React, { useState, useEffect, useCallback } from 'react';
import { useSearchParams } from 'react-router-dom';
import { AlertCircle, RefreshCw, UserCheck } from 'lucide-react';
import { academicService } from '@/services/academicService';
import { coachTaskService, CoachTaskStatus } from '@/services/coachTaskService';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import {
  AcademicStatusItem,
  DayActivityItem,
  LinkedStudentSummary,
  ParentSummaryViewModel,
  UpcomingEventItem,
} from './types';
import { ChildHeroCard } from './components/ChildHeroCard';
import { WeeklyActivityCard } from './components/WeeklyActivityCard';
import { AcademicStatusCard } from './components/AcademicStatusCard';
import { UpcomingCard } from './components/UpcomingCard';

import { AcademicAiSummaryCard } from '@/features/student/academic/components/AcademicAiSummaryCard';
import { QuestionStyleAnalyticsSection } from '@/features/student/academic/components/QuestionStyleAnalyticsSection';

export interface ParentSummaryViewProps {
  initialData?: ParentSummaryViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

const WEEK_DAYS_TR = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];

export const ParentSummaryView: React.FC<ParentSummaryViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const [searchParams] = useSearchParams();
  const requestedStudentId = searchParams.get('studentId');

  const [data, setData] = useState<ParentSummaryViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(
    propIsLoading || (!initialData && !propErrorMessage)
  );
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const fetchParentData = useCallback(async () => {
    if (initialData) return;

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const studentsRes = await academicService.getStudents({ page: 1, pageSize: 20 });
      const items = studentsRes?.items || [];

      if (items.length === 0) {
        setData(undefined);
        return;
      }

      const targetSummary = requestedStudentId
        ? items.find((s) => s.id === requestedStudentId)
        : items[0];

      if (!targetSummary) {
        setData(undefined);
        return;
      }

      const studentId = targetSummary.id;

      const [s360Res, classificationsRes, activePlanRes, coachTasksRes] = await Promise.allSettled([
        academicService.getStudent360(studentId),
        academicService.getSubjectClassifications(studentId),
        academicService.getActiveStudyPlan(studentId),
        coachTaskService.getCoachTasks(studentId),
      ]);

      const s360 = s360Res.status === 'fulfilled' ? s360Res.value : null;
      const baseStudent = s360?.student || targetSummary;
      const classifications =
        classificationsRes.status === 'fulfilled' && Array.isArray(classificationsRes.value)
          ? classificationsRes.value
          : [];
      const activePlan = activePlanRes.status === 'fulfilled' ? activePlanRes.value : null;
      const coachTasks =
        coachTasksRes.status === 'fulfilled' && Array.isArray(coachTasksRes.value)
          ? coachTasksRes.value.filter((t) => t.status !== CoachTaskStatus.Cancelled)
          : [];

      const fullName =
        baseStudent.fullName ||
        `${baseStudent.firstName || ''} ${baseStudent.lastName || ''}`.trim() ||
        'Öğrenci';
      const initials =
        `${baseStudent.firstName?.[0] || ''}${baseStudent.lastName?.[0] || ''}`.toUpperCase() ||
        fullName
          .split(' ')
          .filter(Boolean)
          .map((p) => p[0])
          .join('')
          .toUpperCase()
          .slice(0, 2) ||
        'ÖĞ';

      const primaryCoach = s360?.coaches?.find((c) => c.isPrimary) || s360?.coaches?.[0];

      const linkedStudent: LinkedStudentSummary = {
        id: baseStudent.id,
        name: fullName,
        initials,
        grade: baseStudent.gradeLevel ? `${baseStudent.gradeLevel}. Sınıf` : '12. Sınıf',
        track: baseStudent.schoolName || 'Sayısal',
        examFocus: s360?.examTypes?.[0]?.name || s360?.recentExam?.examType || 'TYT / AYT',
        coachName: primaryCoach?.name || 'Kurum Danışmanı',
        coachTitle: 'Öğrenci Koçu',
        studentNumber: baseStudent.studentNumber ?? null,
      };

      const todayJsDay = new Date().getDay(); // 0=Sun, 1=Mon...
      const todayMondayIdx = todayJsDay === 0 ? 6 : todayJsDay - 1;

      let planAdherencePercentage: number | null = null;
      let hasActivePlan = false;
      let planTitle: string | undefined;
      let completedTasksCount = 0;
      let totalTasksCount = 0;

      const weeklyActivity: DayActivityItem[] = WEEK_DAYS_TR.map((day, idx) => ({
        day,
        done: false,
        hours: 0,
        today: idx === todayMondayIdx,
      }));

      if (activePlan && activePlan.plan) {
        hasActivePlan = true;
        planTitle = `Haftalık Çalışma Planı (${activePlan.plan.planDays} Gün)`;
        const tasks = activePlan.tasks || [];
        totalTasksCount = tasks.length > 0 ? tasks.length : activePlan.plan.totalTaskCount;
        completedTasksCount = tasks.length > 0
          ? tasks.filter((t) => t.isCompleted).length
          : activePlan.plan.completedTaskCount;

        planAdherencePercentage =
          totalTasksCount > 0
            ? Math.round((completedTasksCount / totalTasksCount) * 100)
            : Math.round((activePlan.plan.taskProgressRate || 0) * 100);

        // Daily activity mapping strictly from actual tasks
        for (let idx = 0; idx < 7; idx++) {
          const dayTasks = tasks.filter((t) => t.dayIndex === idx + 1);
          const doneTasks = dayTasks.filter((t) => t.isCompleted);
          const mins = doneTasks.reduce((sum, t) => sum + (t.durationMinutes || 0), 0);
          weeklyActivity[idx] = {
            day: WEEK_DAYS_TR[idx],
            done: doneTasks.length > 0,
            hours: Math.round((mins / 60) * 10) / 10,
            today: idx === todayMondayIdx,
          };
        }
      } else if (coachTasks.length > 0) {
        // Fallback to coach tasks completion count if no study plan exists
        const doneCoachTasks = coachTasks.filter(
          (t) => t.isCompleted || t.status === CoachTaskStatus.Completed
        );
        completedTasksCount = doneCoachTasks.length;
        totalTasksCount = coachTasks.length;
        planAdherencePercentage = Math.round((completedTasksCount / totalTasksCount) * 100);
        planTitle = 'Koçluk Görevleri Takibi';
      }

      const strongSubjects = classifications
        .filter((c) => c.state === 'Strong')
        .map((c) => c.subjectName);
      const developingSubjects = classifications
        .filter((c) => c.state === 'Developing')
        .map((c) => c.subjectName);
      const prioritySubjects = classifications
        .filter((c) => c.state === 'Critical' || c.state === 'Weak')
        .map((c) => c.subjectName);
      const decliningSubjects = classifications
        .filter((c) => c.trendDirection === 'Decreasing')
        .map((c) => c.subjectName);

      const academicStatus: AcademicStatusItem[] = [
        {
          category: 'Güçlenen Alan',
          categoryType: 'strengthening',
          subjects: strongSubjects.length > 0 ? strongSubjects : ['Genel Akademik Takip'],
          detail:
            strongSubjects.length > 0
              ? 'Son denemelerde yüksek doğruluk ve istikrarlı performans gözlemlendi.'
              : 'Deneme verileri arttıkça güçlü dersler burada listelenecektir.',
        },
        {
          category: 'Gelişiyor',
          categoryType: 'developing',
          subjects: developingSubjects.length > 0 ? developingSubjects : ['Düzenli Çalışma'],
          detail: 'Tutarlı çalışma sürüyor, olumlu yönde ilerliyor.',
        },
        {
          category: 'Öncelik',
          categoryType: 'priority',
          subjects: prioritySubjects.length > 0 ? prioritySubjects : ['Konu Tekrarı'],
          detail: 'Bu alanda daha fazla zaman ayrılması önerilmektedir.',
        },
        {
          category: 'Tekrar Öneriliyor',
          categoryType: 'repetition',
          subjects:
            decliningSubjects.length > 0
              ? decliningSubjects
              : prioritySubjects.length > 0
              ? prioritySubjects.slice(0, 2)
              : ['Genel Tekrar'],
          detail: 'Geçmiş konular üzerinde tekrar yapılması faydalı olacak.',
        },
      ];

      const upcomingEvents: UpcomingEventItem[] = [];
      if (s360?.recentExam) {
        upcomingEvents.push({
          id: `exam-${s360.recentExam.latestExamId}`,
          date: new Date(s360.recentExam.takenAt).toLocaleDateString('tr-TR', {
            day: 'numeric',
            month: 'long',
            year: 'numeric',
          }),
          label: s360.recentExam.title,
          note: `Toplam Net: ${s360.recentExam.totalNet}`,
          type: 'exam',
        });
      }

      coachTasks.slice(0, 3).forEach((ct) => {
        upcomingEvents.push({
          id: ct.id,
          date: ct.dueDate
            ? new Date(ct.dueDate).toLocaleDateString('tr-TR', {
                day: 'numeric',
                month: 'long',
                year: 'numeric',
              })
            : 'Haftalık Görev',
          label: ct.title,
          note: ct.isCompleted ? 'Tamamlandı' : 'Koç Görevi',
          type: 'task',
        });
      });

      setData({
        student: linkedStudent,
        lastUpdated: new Date().toLocaleTimeString('tr-TR', {
          hour: '2-digit',
          minute: '2-digit',
        }),
        weeklyActivity,
        planAdherencePercentage,
        hasActivePlan,
        planTitle,
        completedTasksCount,
        totalTasksCount,
        academicStatus,
        upcomingEvents,
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Öğrenci özeti yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData, requestedStudentId]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchParentData();
    }
  }, [initialData, fetchParentData]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchParentData();
    }
  };

  const activeData = data || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="parent-summary-loading">
        <Skeleton className="h-28 w-full rounded-3xl" />
        <Skeleton className="h-44 w-full rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-64 w-full rounded-2xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 select-none p-4" data-testid="parent-summary-error">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Öğrenci Özeti Yüklenemedi"
        >
          {activeError}
        </Alert>
        <div className="text-center pt-2">
          <Button
            variant="primary"
            size="sm"
            onClick={handleRetry}
            leftIcon={<RefreshCw className="h-4 w-4" />}
          >
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  // Empty State
  if (!activeData || !activeData.student) {
    return (
      <div className="max-w-6xl mx-auto select-none space-y-4 p-4" data-testid="parent-summary-empty">
        <div className="mb-2">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Öğrenci Özeti
          </h1>
        </div>
        <EmptyState
          icon={<UserCheck className="h-8 w-8 text-neutral-400" />}
          title="Bağlı Öğrenci Kaydı Bulunamadı"
          description="Hesabınızla ilişkilendirilmiş aktif bir öğrenci kaydı görünmüyor. Lütfen kurumunuzla iletişime geçin."
        />
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="parent-summary-view">
      {/* 1. Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-1">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl font-bold text-navy-950 tracking-tight">
            Öğrenci Özeti
          </h1>
          <p className="text-xs sm:text-sm text-neutral-500 mt-0.5">
            Çocuğunuzun güncel akademik performansı, haftalık çalışma durumu ve koçluk etkinlikleri
          </p>
        </div>
        <div className="text-xs text-neutral-400 self-start sm:self-auto bg-neutral-100/80 px-2.5 py-1 rounded-full border border-neutral-200">
          Son Güncelleme: {activeData.lastUpdated}
        </div>
      </div>

      {/* 2. Child Identity Hero Banner */}
      <ChildHeroCard student={activeData.student} />

      {/* 2. Weekly Progress & Activity Card */}
      <WeeklyActivityCard
        activity={activeData.weeklyActivity}
        planAdherencePercentage={activeData.planAdherencePercentage}
        hasActivePlan={activeData.hasActivePlan}
        planTitle={activeData.planTitle}
        completedTasksCount={activeData.completedTasksCount}
        totalTasksCount={activeData.totalTasksCount}
      />

      {/* 3. AI Academic Summary for Parent */}
      {activeData.student?.id && (
        <AcademicAiSummaryCard studentId={activeData.student.id} audience="parent" />
      )}

      {/* 4. Question Style Analytics for Parent (Read-only) */}
      {activeData.student?.id && (
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs">
          <QuestionStyleAnalyticsSection
            studentId={activeData.student.id}
            isReadOnly
            simplified
          />
        </div>
      )}

      {/* 5. Academic Status & Upcoming Events Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        <AcademicStatusCard items={activeData.academicStatus} />
        <UpcomingCard events={activeData.upcomingEvents} />
      </div>
    </div>
  );
};

export default ParentSummaryView;
