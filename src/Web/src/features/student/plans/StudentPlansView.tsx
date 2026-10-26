import React, { useState, useEffect, useCallback } from 'react';
import { CalendarDays, AlertCircle, RefreshCw, Filter } from 'lucide-react';
import { academicService } from '@/services/academicService';
import { PersistentStudyPlanDetailDto } from '@/features/student/academic/types';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { PlanHeaderCard } from './components/PlanHeaderCard';
import { DaySelectorRow } from './components/DaySelectorRow';
import { PlanTaskCard } from './components/PlanTaskCard';
import { PlanSubjectProgressCard } from './components/PlanSubjectProgressCard';
import { TaskDetailModal } from './components/TaskDetailModal';
import { StudentCoachTasksCard } from './components/StudentCoachTasksCard';
import { DayScheduleSummary, PlanTaskItem, PlanTaskStatus, StudentPlanViewModel, SubjectPlanProgress } from './types';

export interface StudentPlansViewProps {
  initialData?: StudentPlanViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

const DAY_NAMES_TR = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt'];
const SUBJECT_COLORS = [
  'bg-blue-600',
  'bg-emerald-500',
  'bg-amber-500',
  'bg-purple-600',
  'bg-indigo-500',
];

/**
 * Parses YYYY-MM-DD or ISO timestamp into Date components without timezone shift.
 */
function parseSafeLocalDate(dateStr: string): Date {
  const parts = dateStr.split('T')[0].split('-');
  if (parts.length === 3) {
    const y = parseInt(parts[0], 10);
    const m = parseInt(parts[1], 10) - 1;
    const d = parseInt(parts[2], 10);
    return new Date(y, m, d, 12, 0, 0); // Midday prevents midnight timezone rolling
  }
  return new Date(dateStr);
}

function mapPersistentPlanToViewModel(
  detail: PersistentStudyPlanDetailDto,
  coachName: string,
  goalTitle: string
): StudentPlanViewModel {
  const { plan, tasks: rawTasks } = detail;

  // Format today's local string
  const now = new Date();
  const todayDateStr = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`;

  const mappedTasks: PlanTaskItem[] = rawTasks.map((t) => ({
    id: t.id,
    dayIndex: t.dayIndex,
    subject: t.subjectName || (t.nodeType === 'QuestionStyle' ? 'Soru Stili' : 'Genel Çalışma'),
    topic: t.titleSnapshot || t.topicName || t.learningOutcomeName || 'Çalışma Görevi',
    durationMinutes: t.durationMinutes,
    status: t.isCompleted ? 'completed' : 'pending',
    completedAt: t.completedAt
      ? new Date(t.completedAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
      : undefined,
  }));

  const planDaysCount = plan.planDays || 7;
  const days: DayScheduleSummary[] = [];

  // Anchor start date
  const startDateObj = plan.startDate ? parseSafeLocalDate(plan.startDate) : new Date();

  // 0-BASED DAY INDEX: 0, 1, ..., planDaysCount - 1
  for (let d = 0; d < planDaysCount; d++) {
    const dayTasks = rawTasks.filter((t) => t.dayIndex === d);

    // Compute distinct calendar date for day d
    const dObj = new Date(startDateObj.getFullYear(), startDateObj.getMonth(), startDateObj.getDate() + d, 12, 0, 0);
    const dIso = `${dObj.getFullYear()}-${String(dObj.getMonth() + 1).padStart(2, '0')}-${String(dObj.getDate()).padStart(2, '0')}`;

    days.push({
      dayIndex: d,
      dayName: DAY_NAMES_TR[dObj.getDay()] || `${d + 1}. Gün`,
      dateStr: dObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' }),
      taskCount: dayTasks.length,
      completedCount: dayTasks.filter((t) => t.isCompleted).length,
      isToday: dIso === todayDateStr,
    });
  }

  // Subject statistics: planned hours vs completed tasks
  const subjectMap = new Map<string, { completedTasks: number; totalTasks: number; plannedMinutes: number }>();
  for (const t of mappedTasks) {
    const cur = subjectMap.get(t.subject) || { completedTasks: 0, totalTasks: 0, plannedMinutes: 0 };
    cur.totalTasks += 1;
    cur.plannedMinutes += t.durationMinutes;
    if (t.status === 'completed') {
      cur.completedTasks += 1;
    }
    subjectMap.set(t.subject, cur);
  }

  const subjectProgress: SubjectPlanProgress[] = Array.from(subjectMap.entries()).map(([subject, stats], idx) => ({
    subject,
    completedTasks: stats.completedTasks,
    totalTasks: stats.totalTasks,
    completedHours: Math.round((stats.plannedMinutes / 60) * 10) / 10,
    colorClass: SUBJECT_COLORS[idx % SUBJECT_COLORS.length],
  }));

  const completedTasks = mappedTasks.filter((t) => t.status === 'completed').length;
  const totalTasks = mappedTasks.length;
  const completionRate = totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;

  // Format inclusive date range: startDate to (startDate + planDaysCount - 1)
  const inclusiveEndDateObj = new Date(startDateObj.getFullYear(), startDateObj.getMonth(), startDateObj.getDate() + planDaysCount - 1, 12, 0, 0);
  const startStr = startDateObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' });
  const endStr = inclusiveEndDateObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' });

  const initials = coachName
    .split(' ')
    .filter(Boolean)
    .map((p) => p[0])
    .join('')
    .toUpperCase()
    .slice(0, 2) || 'AK';

  return {
    planId: plan.id,
    title: plan.title || 'Aktif Çalışma Planı',
    academicGoal: goalTitle,
    coachName,
    coachAvatarInitials: initials,
    currentWeek: `${planDaysCount} Günlük Program`,
    dateRange: `${startStr} - ${endStr}`,
    completionRate,
    totalTasks,
    completedTasks,
    days,
    tasks: mappedTasks,
    subjectProgress,
  };
}

export const StudentPlansView: React.FC<StudentPlansViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const [resolvedStudentId, setResolvedStudentId] = useState<string | null>(null);
  const [planData, setPlanData] = useState<StudentPlanViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  // Default to Day 0 (first day of plan)
  const [selectedDayIndex, setSelectedDayIndex] = useState<number>(0);
  const [subjectFilter, setSubjectFilter] = useState<string>('');
  const [tasks, setTasks] = useState<PlanTaskItem[]>(initialData?.tasks || []);
  const [activePlanId, setActivePlanId] = useState<string>('');
  const [historyPlans, setHistoryPlans] = useState<Array<{ id: string; title: string; planDays: number; startDate: string }>>([]);
  const [selectedTaskForModal, setSelectedTaskForModal] = useState<PlanTaskItem | null>(null);

  const fetchPlanData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const studentId = await academicService.getCurrentStudentId();
      if (!studentId) {
        setErrorMessage('Öğrenci profili bulunamadı.');
        setIsLoading(false);
        return;
      }
      setResolvedStudentId(studentId);

      const [detail, historyList] = await Promise.all([
        academicService.getActiveStudyPlan(studentId),
        academicService.listStudyPlans(studentId).catch(() => []),
      ]);

      setHistoryPlans(historyList || []);

      if (detail && detail.plan) {
        setActivePlanId(detail.plan.id);
        const vm = mapPersistentPlanToViewModel(
          detail,
          'Ahmet Koç',
          'TYT & AYT Hedef Geliştirme'
        );
        setPlanData(vm);
        setTasks(vm.tasks);

        // Find today's dayIndex if it exists in the plan, else default to 0
        const todayDay = vm.days.find((d) => d.isToday);
        if (todayDay !== undefined) {
          setSelectedDayIndex(todayDay.dayIndex);
        } else {
          setSelectedDayIndex(0);
        }
      } else {
        setPlanData(undefined);
        setTasks([]);
      }
    } catch (err: any) {
      setErrorMessage(err?.response?.data?.detail || err?.message || 'Çalışma planı yüklenirken bir sorun oluştu.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  const handleSelectHistoryPlan = async (planId: string) => {
    if (!resolvedStudentId || !planId) return;
    setIsLoading(true);
    try {
      setActivePlanId(planId);
      const detail = await academicService.getStudyPlanDetail(resolvedStudentId, planId);
      if (detail && detail.plan) {
        const vm = mapPersistentPlanToViewModel(
          detail,
          'Ahmet Koç',
          'Geçmiş Çalışma Programı'
        );
        setPlanData(vm);
        setTasks(vm.tasks);
        setSelectedDayIndex(0);
      }
    } catch (err: any) {
      setErrorMessage('Seçilen plan yüklenemedi.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    if (initialData) {
      setPlanData(initialData);
      setTasks(initialData.tasks || []);
      setIsLoading(false);
    } else {
      fetchPlanData();
    }
  }, [initialData, fetchPlanData]);

  // Status toggle handler
  const handleToggleStatus = async (id: string) => {
    const target = tasks.find((t) => t.id === id);
    if (!target) return;

    const activeVm = planData || initialData;

    if (resolvedStudentId && activeVm?.planId && !initialData) {
      const nextCompleted = target.status !== 'completed';
      try {
        const updated = await academicService.updateTaskCompletion(
          resolvedStudentId,
          activeVm.planId,
          id,
          nextCompleted
        );
        const newStatus: PlanTaskStatus = updated.isCompleted ? 'completed' : 'pending';
        const newCompletedAt = updated.completedAt
          ? new Date(updated.completedAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
          : undefined;

        setTasks((prev) =>
          prev.map((t) => (t.id === id ? { ...t, status: newStatus, completedAt: newCompletedAt } : t))
        );
        setPlanData((prev) => {
          if (!prev) return prev;
          const nextTasks = prev.tasks.map((t) =>
            t.id === id ? { ...t, status: newStatus, completedAt: newCompletedAt } : t
          );
          const completedTasks = nextTasks.filter((t) => t.status === 'completed').length;
          const completionRate = nextTasks.length > 0 ? Math.round((completedTasks / nextTasks.length) * 100) : 0;
          const nextDays = prev.days.map((d) => {
            const dTasks = nextTasks.filter((t) => t.dayIndex === d.dayIndex);
            return {
              ...d,
              taskCount: dTasks.length,
              completedCount: dTasks.filter((t) => t.status === 'completed').length,
            };
          });
          return {
            ...prev,
            tasks: nextTasks,
            completedTasks,
            completionRate,
            days: nextDays,
          };
        });
      } catch {
        // Fallback
      }
    } else {
      const newStatus: PlanTaskStatus = target.status === 'completed' ? 'pending' : 'completed';
      setTasks((prev) => prev.map((t) => (t.id === id ? { ...t, status: newStatus } : t)));
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-4 max-w-5xl mx-auto select-none" data-testid="student-plans-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <Skeleton className="h-16 w-full rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="md:col-span-2 space-y-3">
            <Skeleton className="h-20 w-full rounded-2xl" />
            <Skeleton className="h-20 w-full rounded-2xl" />
          </div>
          <Skeleton className="h-48 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  if (errorMessage && !planData) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none" data-testid="student-plans-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Plan Yüklenemedi">
          {errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={onRetry || fetchPlanData} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  if (!planData) {
    return (
      <div className="max-w-5xl mx-auto py-12 select-none" data-testid="student-plans-empty">
        <EmptyState
          icon={<CalendarDays className="h-12 w-12 text-neutral-300" />}
          title="Aktif Çalışma Planı Bulunmuyor"
          description="Akademik analiz sayfasından zayıf konularına özel haftalık plan teklifi oluşturabilir veya koçunun senin için hazırlayacağı programı bekleyebilirsin."
          action={
            <Button
              variant="primary"
              size="sm"
              onClick={() => (window.location.href = '/student/academic')}
            >
              Akademik Analize Git
            </Button>
          }
        />
      </div>
    );
  }

  // Selected Day Tasks (Filtered by selected 0-based dayIndex)
  const currentDayTasks = tasks.filter((t) => {
    const matchesDay = t.dayIndex === selectedDayIndex;
    const matchesSubject = !subjectFilter || t.subject === subjectFilter;
    return matchesDay && matchesSubject;
  });

  const subjectsInPlan = Array.from(new Set(tasks.map((t) => t.subject)));

  return (
    <div className="max-w-5xl mx-auto space-y-4 sm:space-y-5 select-none" data-testid="student-plans-view">
      {/* 1. Header Row & Plan History Switcher */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-200/60 pb-3 sm:pb-4">
        <div>
          <h1 className="text-xl sm:text-2xl font-bold text-[#14233E] tracking-tight">
            Çalışma Planım
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Haftalık hedeflerin ve koçunun atadığı çalışma görevleri
          </p>
        </div>

        {historyPlans.length > 1 && (
          <div className="flex items-center gap-2">
            <span className="text-xs text-neutral-400">Plan Seç:</span>
            <Select
              options={historyPlans.map((hp) => ({
                value: hp.id,
                label: `${hp.title || 'Plan'} (${new Date(hp.startDate).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' })})`,
              }))}
              value={activePlanId}
              onChange={(e) => handleSelectHistoryPlan(e.target.value)}
              className="text-xs min-h-[36px] py-1 bg-white border-neutral-200/80 rounded-xl"
            />
          </div>
        )}
      </div>

      {/* 2. Compact Navy Plan Header Card */}
      <PlanHeaderCard plan={planData} />

      {/* 3. Seven-Day Schedule Selector Row (0-based indexing) */}
      <DaySelectorRow
        days={planData.days}
        selectedDayIndex={selectedDayIndex}
        onSelectDay={(idx) => setSelectedDayIndex(idx)}
      />

      {/* 4. Main Two-Column Layout (Tasks on Left, Sidebars on Right) */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 sm:gap-5">
        {/* Left Column (lg:col-span-8): Daily Tasks & Coach Tasks */}
        <div className="lg:col-span-8 space-y-4">
          {/* Day Task Header & Filter */}
          <div className="flex items-center justify-between">
            <div>
              <h3 className="text-sm font-bold text-[#14233E]">
                {planData.days.find((d) => d.dayIndex === selectedDayIndex)?.dayName || `${selectedDayIndex + 1}. Gün`} Görevleri
              </h3>
              <span className="text-[11px] text-neutral-400">
                {planData.days.find((d) => d.dayIndex === selectedDayIndex)?.dateStr} • {currentDayTasks.filter((t) => t.status === 'completed').length}/{currentDayTasks.length} Tamamlandı
              </span>
            </div>

            {subjectsInPlan.length > 1 && (
              <div className="flex items-center gap-1.5">
                <Filter className="w-3.5 h-3.5 text-neutral-400" />
                <select
                  value={subjectFilter}
                  onChange={(e) => setSubjectFilter(e.target.value)}
                  className="text-xs bg-white border border-neutral-200/80 rounded-xl px-2.5 py-1.5 text-neutral-700 focus:outline-none focus:ring-1 focus:ring-[#315FE8]"
                >
                  <option value="">Tüm Dersler</option>
                  {subjectsInPlan.map((s) => (
                    <option key={s} value={s}>
                      {s}
                    </option>
                  ))}
                </select>
              </div>
            )}
          </div>

          {/* Daily Task List */}
          {currentDayTasks.length > 0 ? (
            <div className="space-y-3">
              {currentDayTasks.map((task) => (
                <PlanTaskCard
                  key={task.id}
                  task={task}
                  onToggleStatus={handleToggleStatus}
                  onViewDetails={(t) => setSelectedTaskForModal(t)}
                />
              ))}
            </div>
          ) : (
            <div className="bg-white rounded-2xl border border-neutral-200/70 p-6 text-center text-xs text-neutral-400">
              Bu gün için planlanmış çalışma görevi bulunmuyor. Diğer günleri inceleyebilirsin.
            </div>
          )}

          {/* Dedicated Coach Tasks Section */}
          <StudentCoachTasksCard studentId={resolvedStudentId || undefined} />
        </div>

        {/* Right Column (lg:col-span-4): Subject Distribution & Progress */}
        <div className="lg:col-span-4 space-y-4">
          <PlanSubjectProgressCard subjects={planData.subjectProgress} />
        </div>
      </div>

      {/* Task Details Modal */}
      {selectedTaskForModal && (
        <TaskDetailModal
          task={selectedTaskForModal}
          isOpen={true}
          onClose={() => setSelectedTaskForModal(null)}
          onToggleStatus={(id) => {
            handleToggleStatus(id);
            setSelectedTaskForModal(null);
          }}
        />
      )}
    </div>
  );
};
