import React, { useState, useEffect, useCallback } from 'react';
import { CalendarDays, AlertCircle, RefreshCw } from 'lucide-react';
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
  'bg-primary-500',
  'bg-success',
  'bg-warning',
  'bg-attention',
  'bg-info',
];

function mapPersistentPlanToViewModel(
  detail: PersistentStudyPlanDetailDto,
  coachName: string,
  goalTitle: string
): StudentPlanViewModel {
  const { plan, tasks: rawTasks } = detail;
  const todayIso = new Date().toISOString().split('T')[0];

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

  const totalDays = Math.max(plan.planDays || 7, ...rawTasks.map((t) => t.dayIndex), 1);
  const days: DayScheduleSummary[] = [];
  const startDateObj = plan.startDate ? new Date(plan.startDate) : new Date();

  for (let d = 1; d <= totalDays; d++) {
    const dayTasks = rawTasks.filter((t) => t.dayIndex === d);
    const sampleDateStr = dayTasks.find((t) => t.plannedDate)?.plannedDate;
    const dObj = sampleDateStr
      ? new Date(sampleDateStr)
      : new Date(startDateObj.getTime() + (d - 1) * 86400000);
    const dIso = dObj.toISOString().split('T')[0];

    days.push({
      dayIndex: d,
      dayName: DAY_NAMES_TR[dObj.getDay()] || `${d}. Gün`,
      dateStr: dObj.toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' }),
      taskCount: dayTasks.length,
      completedCount: dayTasks.filter((t) => t.isCompleted).length,
      isToday: dIso === todayIso,
    });
  }

  const subjectMap = new Map<string, { completedTasks: number; totalTasks: number; completedMinutes: number }>();
  for (const t of mappedTasks) {
    const cur = subjectMap.get(t.subject) || { completedTasks: 0, totalTasks: 0, completedMinutes: 0 };
    cur.totalTasks += 1;
    if (t.status === 'completed') {
      cur.completedTasks += 1;
      cur.completedMinutes += t.durationMinutes;
    }
    subjectMap.set(t.subject, cur);
  }

  const subjectProgress: SubjectPlanProgress[] = Array.from(subjectMap.entries()).map(([subject, stats], idx) => ({
    subject,
    completedTasks: stats.completedTasks,
    totalTasks: stats.totalTasks,
    completedHours: Math.round((stats.completedMinutes / 60) * 10) / 10,
    colorClass: SUBJECT_COLORS[idx % SUBJECT_COLORS.length],
  }));

  const completedTasks = mappedTasks.filter((t) => t.status === 'completed').length;
  const totalTasks = mappedTasks.length;
  const completionRate = totalTasks > 0 ? Math.round((completedTasks / totalTasks) * 100) : 0;

  const startStr = plan.startDate ? new Date(plan.startDate).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' }) : '';
  const endStr = plan.endDate ? new Date(plan.endDate).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' }) : '';

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
    currentWeek: `${plan.planDays} Günlük Program`,
    dateRange: startStr && endStr ? `${startStr} - ${endStr}` : 'Aktif Dönem',
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

  const [selectedDayIndex, setSelectedDayIndex] = useState<number>(1);
  const [subjectFilter, setSubjectFilter] = useState<string>('');
  const [tasks, setTasks] = useState<PlanTaskItem[]>(initialData?.tasks || []);
  const [selectedTask, setSelectedTask] = useState<PlanTaskItem | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState<boolean>(false);

  const fetchPlanData = useCallback(async () => {
    if (initialData) return;

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const studentId = await academicService.getCurrentStudentId();
      setResolvedStudentId(studentId);

      if (!studentId) {
        setPlanData(undefined);
        setTasks([]);
        return;
      }

      const [activePlanRes, student360Res] = await Promise.allSettled([
        academicService.getActiveStudyPlan(studentId),
        academicService.getStudent360(studentId),
      ]);

      if (activePlanRes.status === 'fulfilled' && activePlanRes.value && activePlanRes.value.tasks?.length > 0) {
        const s360 = student360Res.status === 'fulfilled' ? student360Res.value : null;
        const primaryCoach = s360?.coaches?.find((c) => c.isPrimary) || s360?.coaches?.[0];
        const coachName = primaryCoach?.name || 'Akademik Danışman';
        const goalTitle = s360?.currentGoals?.[0]?.title || 'Akademik Zeka Hedef Programı';

        const vm = mapPersistentPlanToViewModel(activePlanRes.value, coachName, goalTitle);
        setPlanData(vm);
        setTasks(vm.tasks);

        const todayDay = vm.days.find((d) => d.isToday) || vm.days[0];
        if (todayDay) {
          setSelectedDayIndex(todayDay.dayIndex);
        }
      } else {
        setPlanData(undefined);
        setTasks([]);
      }
    } catch {
      setPlanData(undefined);
      setTasks([]);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

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
        // Fallback to local toggle if API fails
      }
      return;
    }

    setTasks((prev) =>
      prev.map((t) => {
        if (t.id === id) {
          if (t.status === 'completed') {
            return { ...t, status: 'pending', completedAt: undefined };
          } else if (t.status === 'pending') {
            return { ...t, status: 'in_progress' };
          } else {
            return {
              ...t,
              status: 'completed',
              completedAt: new Date().toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit',
              }),
            };
          }
        }
        return t;
      })
    );
  };

  const handleOpenDetail = (task: PlanTaskItem) => {
    setSelectedTask(task);
    setIsDetailOpen(true);
  };

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchPlanData();
    }
  };

  const activeData = planData || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="student-plans-loading">
        <Skeleton className="h-44 w-full rounded-2xl" />
        <Skeleton className="h-16 w-full rounded-2xl" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="lg:col-span-2 space-y-3">
            <Skeleton className="h-24 w-full rounded-2xl" />
            <Skeleton className="h-24 w-full rounded-2xl" />
            <Skeleton className="h-24 w-full rounded-2xl" />
          </div>
          <div>
            <Skeleton className="h-64 w-full rounded-2xl" />
          </div>
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4" data-testid="student-plans-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Plan Yüklenemedi">
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

  const hasTasks = activeData && tasks.length > 0;

  if (!hasTasks) {
    return (
      <div className="max-w-5xl mx-auto select-none space-y-4" data-testid="student-plans-view">
        <div className="border-b border-neutral-100 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Çalışma Planları
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Haftalık ve günlük akademik hedefler
          </p>
        </div>

        {/* Always render Coach Tasks Card so coach-assigned tasks are visible even without an active weekly plan */}
        <StudentCoachTasksCard studentId={resolvedStudentId || undefined} />

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-6 sm:p-8 shadow-soft-sm text-center">
          <div className="w-12 h-12 rounded-2xl bg-navy-50 text-navy-800 flex items-center justify-center mx-auto mb-4 border border-navy-100">
            <CalendarDays className="h-6 w-6 text-primary-600" />
          </div>
          <h2 className="text-lg font-semibold text-neutral-900">
            Aktif Çalışma Planı Bulunmuyor
          </h2>
          <p className="text-xs sm:text-sm text-neutral-600 max-w-md mx-auto mt-2 leading-relaxed">
            Sizin için atanmış bir haftalık çalışma planı henüz bulunmuyor. Danışman koçunuz veya branş öğretmeniniz program hazırladığında ders ve konu bazlı görevleriniz burada listelenecektir.
          </p>
        </div>
      </div>
    );
  }

  const filteredTasks = tasks.filter((t) => {
    const matchesDay = t.dayIndex === selectedDayIndex;
    const matchesSubject = !subjectFilter || t.subject === subjectFilter;
    return matchesDay && matchesSubject;
  });

  const selectedDay = activeData.days.find((d) => d.dayIndex === selectedDayIndex) || activeData.days[0];
  const uniqueSubjects = Array.from(new Set(tasks.map((t) => t.subject).filter(Boolean)));
  const subjectOptions =
    uniqueSubjects.length > 0
      ? uniqueSubjects.map((s) => ({ value: s, label: s }))
      : [
          { value: 'Matematik', label: 'Matematik' },
          { value: 'Türkçe', label: 'Türkçe' },
          { value: 'Fizik', label: 'Fizik' },
          { value: 'Kimya', label: 'Kimya' },
        ];

  return (
    <div className="max-w-5xl mx-auto select-none space-y-3.5 sm:space-y-4" data-testid="student-plans-view">
      {/* 1. Plan Overview Hero Card */}
      <PlanHeaderCard plan={activeData} />

      {/* 2. Day Selector */}
      <DaySelectorRow
        days={activeData.days}
        selectedDayIndex={selectedDayIndex}
        onSelectDay={setSelectedDayIndex}
      />

      {/* 3. Main Content & Sidebar */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-4">
        {/* Left Column: Daily Tasks */}
        <div className="lg:col-span-2 space-y-3">
          {/* Koç Görevleri Section */}
          <StudentCoachTasksCard studentId={resolvedStudentId || undefined} />

          {/* Day Title & Filter Bar */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 bg-white rounded-2xl border border-neutral-100 p-3 sm:px-4">
            <div className="flex items-center gap-2">
              <CalendarDays className="h-4 w-4 text-primary-600" />
              <h3 className="text-sm font-semibold text-neutral-900">
                {selectedDay?.dayName} Günü Görevleri ({selectedDay?.dateStr})
              </h3>
            </div>

            <div className="flex items-center gap-2">
              <Select
                options={subjectOptions}
                value={subjectFilter}
                onChange={(e) => setSubjectFilter(e.target.value)}
                placeholder="Tüm Dersler"
                className="text-xs min-h-[34px] py-1 pl-2.5 pr-7"
              />
            </div>
          </div>

          {/* Task List or Empty State */}
          {filteredTasks.length > 0 ? (
            <div className="space-y-2.5">
              {filteredTasks.map((task) => (
                <PlanTaskCard
                  key={task.id}
                  task={task}
                  onToggleStatus={handleToggleStatus}
                  onViewDetails={handleOpenDetail}
                />
              ))}
            </div>
          ) : (
            <EmptyState
              icon={<CalendarDays className="h-6 w-6 text-neutral-400" />}
              title="Bu Gün İçin Görev Yok"
              description={
                subjectFilter
                  ? `Seçili filtreye (${subjectFilter}) uygun görev bulunamadı.`
                  : 'Bu gün için planlanmış herhangi bir çalışma görevi bulunmuyor.'
              }
              action={
                subjectFilter ? (
                  <Button size="sm" variant="secondary" onClick={() => setSubjectFilter('')}>
                    Filtreyi Temizle
                  </Button>
                ) : undefined
              }
            />
          )}
        </div>

        {/* Right Sidebar: Subject Progress Targets */}
        <div className="space-y-3.5 sm:space-y-4">
          <PlanSubjectProgressCard subjects={activeData.subjectProgress} />
        </div>
      </div>

      {/* Task Detail Modal */}
      <TaskDetailModal
        task={selectedTask}
        isOpen={isDetailOpen}
        onClose={() => setIsDetailOpen(false)}
        onToggleStatus={handleToggleStatus}
      />
    </div>
  );
};
