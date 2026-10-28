import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Users,
  CalendarCheck,
  CheckCircle2,
  ListTodo,
  AlertCircle,
  RefreshCw,
  ChevronRight,
  TrendingUp,
  GraduationCap,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { useAuth } from '@/auth';
import { academicService, StudentSummaryDto } from '@/services/academicService';
import { coachTaskService, CoachTaskDto } from '@/services/coachTaskService';
import { CoachReportsViewModel, StudentReportItem, GradeDistributionItem } from './types';

export interface CoachReportsViewProps {
  initialData?: CoachReportsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const CoachReportsView: React.FC<CoachReportsViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [data, setData] = useState<CoachReportsViewModel | null>(initialData || null);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);
  const [gradeFilter, setGradeFilter] = useState<string>('all');

  const fetchReportsData = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      // 1. Fetch assigned students (limit to 50 for bounded fetching)
      const studentsRes = await academicService.getStudents({ page: 1, pageSize: 50 });
      const students: StudentSummaryDto[] = studentsRes?.items || [];

      if (students.length === 0) {
        setData({
          totalAssignedStudents: 0,
          activePlansCount: 0,
          averagePlanCompletion: null,
          totalCoachTasks: 0,
          completedCoachTasks: 0,
          gradeBreakdown: [],
          studentReports: [],
        });
        return;
      }

      // 2. Fetch plans & tasks with bounded concurrency
      const studentReports: StudentReportItem[] = await Promise.all(
        students.map(async (student) => {
          let hasActivePlan = false;
          let planTitle: string | undefined;
          let planCompletionRate: number | undefined;
          let totalTasks: number | undefined;
          let completedTasks: number | undefined;

          try {
            const planDetail = await academicService.getActiveStudyPlan(student.id);
            if (planDetail && planDetail.plan) {
              hasActivePlan = true;
              planTitle = `Çalışma Planı (${planDetail.plan.planDays} Gün)`;
              const tasks = planDetail.tasks || [];
              totalTasks = tasks.length > 0 ? tasks.length : planDetail.plan.totalTaskCount;
              completedTasks = tasks.length > 0
                ? tasks.filter((t) => t.isCompleted).length
                : planDetail.plan.completedTaskCount;

              planCompletionRate =
                totalTasks > 0
                  ? Math.round((completedTasks / totalTasks) * 100)
                  : Math.round(planDetail.plan.taskProgressRate * 100);
            }
          } catch {
            // Student has no active plan or fetch failed gracefully
          }

          let coachTasksTotal = 0;
          let coachTasksCompleted = 0;
          try {
            const cTasks: CoachTaskDto[] = await coachTaskService.getCoachTasks(student.id);
            if (Array.isArray(cTasks)) {
              coachTasksTotal = cTasks.length;
              coachTasksCompleted = cTasks.filter((t) => t.isCompleted).length;
            }
          } catch {
            // Task fetch failed gracefully
          }

          return {
            id: student.id,
            fullName: student.fullName || `${student.firstName} ${student.lastName}`.trim(),
            gradeLevel: student.gradeLevel ?? null,
            hasActivePlan,
            planTitle,
            planCompletionRate,
            totalTasks,
            completedTasks,
            coachTasksTotal,
            coachTasksCompleted,
          };
        })
      );

      // 3. Aggregate KPIs
      const totalAssignedStudents = studentReports.length;
      const studentsWithPlans = studentReports.filter((s) => s.hasActivePlan && s.planCompletionRate !== undefined);
      const activePlansCount = studentsWithPlans.length;

      const averagePlanCompletion =
        activePlansCount > 0
          ? Math.round(studentsWithPlans.reduce((sum, s) => sum + (s.planCompletionRate || 0), 0) / activePlansCount)
          : null;

      const totalCoachTasks = studentReports.reduce((sum, s) => sum + s.coachTasksTotal, 0);
      const completedCoachTasks = studentReports.reduce((sum, s) => sum + s.coachTasksCompleted, 0);

      // 4. Grade-level Breakdown
      const gradeGroups = new Map<number | string, StudentReportItem[]>();
      studentReports.forEach((s) => {
        const key = s.gradeLevel ? `${s.gradeLevel}. Sınıf` : 'Belirtilmemiş';
        if (!gradeGroups.has(key)) gradeGroups.set(key, []);
        gradeGroups.get(key)!.push(s);
      });

      const gradeBreakdown: GradeDistributionItem[] = Array.from(gradeGroups.entries()).map(([gradeName, list]) => {
        const withPlan = list.filter((s) => s.hasActivePlan && s.planCompletionRate !== undefined);
        const avg =
          withPlan.length > 0
            ? Math.round(withPlan.reduce((sum, s) => sum + (s.planCompletionRate || 0), 0) / withPlan.length)
            : null;

        return {
          gradeName: String(gradeName),
          studentCount: list.length,
          studentsWithPlan: withPlan.length,
          averageAdherence: avg,
        };
      });

      setData({
        totalAssignedStudents,
        activePlansCount,
        averagePlanCompletion,
        totalCoachTasks,
        completedCoachTasks,
        gradeBreakdown,
        studentReports,
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Portföy verileri yüklenemedi.';
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
      fetchReportsData();
    }
  }, [initialData, fetchReportsData]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchReportsData();
    }
  };

  // Filtered Students
  const filteredStudents = useMemo(() => {
    if (!data) return [];
    if (gradeFilter === 'all') return data.studentReports;
    return data.studentReports.filter((s) => {
      const g = s.gradeLevel ? `${s.gradeLevel}. Sınıf` : 'Belirtilmemiş';
      return g === gradeFilter;
    });
  }, [data, gradeFilter]);

  // Loading State
  if (propIsLoading || isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="coach-reports-loading">
        <Skeleton className="h-28 w-full rounded-3xl" />
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
        </div>
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (propErrorMessage || errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 select-none p-4" data-testid="coach-reports-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Raporlar Yüklenemedi">
          {propErrorMessage || errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const report = data || {
    totalAssignedStudents: 0,
    activePlansCount: 0,
    averagePlanCompletion: null,
    totalCoachTasks: 0,
    completedCoachTasks: 0,
    gradeBreakdown: [],
    studentReports: [],
  };

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="coach-reports-view">
      {/* 1. Header Hero Banner (Navy #14233E) */}
      <div className="bg-[#14233E] rounded-3xl p-5 sm:p-7 text-white shadow-soft-sm relative overflow-hidden border border-navy-800">
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1.5">
              <span className="text-xs px-2.5 py-0.5 rounded-full bg-navy-800/80 text-blue-300 border border-navy-700 font-medium">
                {user?.institutionName || 'Kurum Portalı'}
              </span>
              <span className="text-navy-500">•</span>
              <span className="text-xs font-mono text-navy-300">
                {report.totalAssignedStudents} Atanmış Öğrenci
              </span>
            </div>
            <h1 className="font-serif text-2xl sm:text-3xl font-bold tracking-tight text-white leading-tight">
              Portföy Gelişim Raporu
            </h1>
            <p className="text-xs sm:text-sm text-navy-200 mt-1 max-w-xl leading-relaxed">
              Atanmış öğrencilerinizin gerçek çalışma planı uyumu, hedef ilerlemesi ve koçluk görevleri takibi.
            </p>
          </div>

          <div className="flex items-center gap-2.5 shrink-0">
            <Button
              variant="outline"
              size="sm"
              onClick={handleRetry}
              leftIcon={<RefreshCw className="h-3.5 w-3.5" />}
              className="text-white border-navy-700 hover:bg-navy-800 text-xs rounded-xl"
            >
              Yenile
            </Button>
          </div>
        </div>
      </div>

      {/* 2. Top Summary KPI Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 sm:gap-4">
        {/* Total Students */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Atanmış Öğrenci</span>
            <Users className="h-4 w-4 text-primary-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{report.totalAssignedStudents}</span>
            <span className="text-xs text-neutral-500 font-medium">öğrenci</span>
          </div>
          <p className="text-[11px] text-neutral-400 mt-1">Aktif danışmanlık portföyü</p>
        </div>

        {/* Active Plans */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Aktif Planlar</span>
            <CalendarCheck className="h-4 w-4 text-[#315FE8]" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{report.activePlansCount}</span>
            <span className="text-xs text-neutral-500 font-medium">/ {report.totalAssignedStudents}</span>
          </div>
          <p className="text-[11px] text-neutral-400 mt-1">
            {report.totalAssignedStudents > 0
              ? `%${Math.round((report.activePlansCount / report.totalAssignedStudents) * 100)} kapsama`
              : 'Öğrenci yok'}
          </p>
        </div>

        {/* Average Plan Completion */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Ortalama Plan Uyumu</span>
            <TrendingUp className="h-4 w-4 text-emerald-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            {report.averagePlanCompletion !== null ? (
              <>
                <span className="text-2xl font-bold text-emerald-600 font-mono">
                  %{report.averagePlanCompletion}
                </span>
                <span className="text-xs text-neutral-500 font-medium">uyum</span>
              </>
            ) : (
              <span className="text-sm font-semibold text-neutral-400">Aktif Plan Yok</span>
            )}
          </div>
          <p className="text-[11px] text-neutral-400 mt-1">Yalnızca aktif planı olanlar</p>
        </div>

        {/* Coach Tasks Completion */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Koçluk Görevleri</span>
            <ListTodo className="h-4 w-4 text-indigo-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">
              {report.completedCoachTasks}
            </span>
            <span className="text-xs text-neutral-500 font-medium">/ {report.totalCoachTasks}</span>
          </div>
          <p className="text-[11px] text-neutral-400 mt-1">
            {report.totalCoachTasks > 0
              ? `%${Math.round((report.completedCoachTasks / report.totalCoachTasks) * 100)} tamamlandı`
              : 'Görev atanmamış'}
          </p>
        </div>
      </div>

      {/* 3. Grade-level Adherence Breakdown */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs space-y-4">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <GraduationCap className="h-4 w-4 text-primary-600" />
            <h3 className="text-sm sm:text-base font-bold text-neutral-900">
              Sınıf Bazında Portföy Dağılımı ve Plan Uyumu
            </h3>
          </div>
        </div>

        {report.gradeBreakdown.length === 0 ? (
          <EmptyState
            icon={<GraduationCap className="h-6 w-6 text-neutral-400" />}
            title="Sınıf Dağılımı Bulunamadı"
            description="Atanmış öğrenci kaydı bulunmamaktadır."
          />
        ) : (
          <div className="space-y-3.5">
            {report.gradeBreakdown.map((item) => (
              <div key={item.gradeName} className="space-y-1.5">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between text-xs gap-1">
                  <span className="font-semibold text-neutral-800">{item.gradeName}</span>
                  <div className="flex items-center gap-3 text-neutral-500">
                    <span>{item.studentCount} Öğrenci</span>
                    <span>{item.studentsWithPlan} Aktif Plan</span>
                    <span className="font-mono font-bold text-neutral-900">
                      {item.averageAdherence !== null ? `%${item.averageAdherence} Uyum` : 'Plan Yok'}
                    </span>
                  </div>
                </div>

                <div className="h-2 w-full bg-neutral-100 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-[#315FE8] rounded-full transition-all duration-500"
                    style={{ width: `${item.averageAdherence ?? 0}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* 4. Student-Level Progress Directory */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
          <div>
            <h3 className="text-sm sm:text-base font-bold text-neutral-900">
              Öğrenci Gelişim ve Çalışma Durumları
            </h3>
            <p className="text-xs text-neutral-500 mt-0.5">
              Portföyünüzdeki öğrencilerin detaylı plan ve görev analizleri
            </p>
          </div>

          {/* Grade filter */}
          {report.gradeBreakdown.length > 0 && (
            <div className="flex items-center gap-1.5 flex-wrap">
              <button
                type="button"
                onClick={() => setGradeFilter('all')}
                className={`text-xs px-2.5 py-1 rounded-lg font-medium transition-colors ${
                  gradeFilter === 'all'
                    ? 'bg-navy-900 text-white'
                    : 'bg-neutral-100 text-neutral-600 hover:bg-neutral-200'
                }`}
              >
                Tümü ({report.studentReports.length})
              </button>
              {report.gradeBreakdown.map((g) => (
                <button
                  key={g.gradeName}
                  type="button"
                  onClick={() => setGradeFilter(g.gradeName)}
                  className={`text-xs px-2.5 py-1 rounded-lg font-medium transition-colors ${
                    gradeFilter === g.gradeName
                      ? 'bg-navy-900 text-white'
                      : 'bg-neutral-100 text-neutral-600 hover:bg-neutral-200'
                  }`}
                >
                  {g.gradeName}
                </button>
              ))}
            </div>
          )}
        </div>

        {filteredStudents.length === 0 ? (
          <EmptyState
            icon={<Users className="h-6 w-6 text-neutral-400" />}
            title="Öğrenci Bulunamadı"
            description="Seçilen filtre kriterlerine uygun öğrenci kaydı bulunmamaktadır."
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            {filteredStudents.map((student) => (
              <div
                key={student.id}
                onClick={() => navigate(`/coach/students/${student.id}`)}
                className="bg-[#F8F7F3] rounded-2xl p-4 border border-neutral-200/80 shadow-soft-xs hover:border-primary-300 transition-all cursor-pointer flex flex-col justify-between space-y-3"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="min-w-0">
                    <h4 className="text-sm font-bold text-neutral-900 truncate hover:text-primary-600 transition-colors">
                      {student.fullName}
                    </h4>
                    <p className="text-xs text-neutral-500 mt-0.5">
                      {student.gradeLevel ? `${student.gradeLevel}. Sınıf` : 'Sınıf Belirtilmemiş'}
                    </p>
                  </div>
                  <ChevronRight className="h-4 w-4 text-neutral-400 shrink-0 mt-0.5" />
                </div>

                {/* Plan Status */}
                <div className="space-y-1.5 bg-white p-3 rounded-xl border border-neutral-200/60">
                  <div className="flex items-center justify-between text-xs">
                    <span className="text-neutral-600 font-medium truncate">
                      {student.hasActivePlan ? student.planTitle || 'Aktif Çalışma Planı' : 'Aktif Plan Yok'}
                    </span>
                    <span className="font-mono font-bold text-neutral-900 shrink-0">
                      {student.planCompletionRate !== undefined ? `%${student.planCompletionRate}` : '—'}
                    </span>
                  </div>

                  <div className="h-1.5 w-full bg-neutral-100 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all duration-300 ${
                        (student.planCompletionRate ?? 0) >= 70
                          ? 'bg-emerald-500'
                          : (student.planCompletionRate ?? 0) >= 40
                          ? 'bg-amber-500'
                          : 'bg-rose-500'
                      }`}
                      style={{ width: `${student.planCompletionRate ?? 0}%` }}
                    />
                  </div>

                  {student.totalTasks !== undefined && (
                    <div className="flex justify-between text-[11px] text-neutral-400">
                      <span>{student.completedTasks} / {student.totalTasks} görev tamamlandı</span>
                      <span>Plan Uyumu</span>
                    </div>
                  )}
                </div>

                {/* Coach Task Pill */}
                <div className="flex items-center justify-between text-xs pt-1 border-t border-neutral-200/60 text-neutral-500">
                  <span className="flex items-center gap-1.5">
                    <CheckCircle2 className="h-3.5 w-3.5 text-indigo-600" />
                    <span>Koçluk Görevleri:</span>
                  </span>
                  <span className="font-mono font-semibold text-neutral-800">
                    {student.coachTasksCompleted} / {student.coachTasksTotal}
                  </span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default CoachReportsView;
