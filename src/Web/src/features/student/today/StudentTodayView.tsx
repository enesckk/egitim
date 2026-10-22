import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  CalendarDays,
  FileText,
  User,
  Sparkles,
  AlertCircle,
  RefreshCw,
  BookOpen,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/auth';
import { academicService } from '@/services/academicService';
import { coachTaskService, CoachTaskStatus } from '@/services/coachTaskService';
import { getRecommendationTypeLabel } from '@/features/student/academic/types';
import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import { Skeleton } from '@/components/ui/Skeleton';
import { NextStudyCard } from './components/NextStudyCard';
import { TodayPlanTab } from './components/TodayPlanTab';
import { WeeklyProgressTab } from './components/WeeklyProgressTab';
import { RecommendationCard } from './components/RecommendationCard';
import { UpcomingCard } from './components/UpcomingCard';
import { MonthlyStatsCard } from './components/MonthlyStatsCard';
import {
  DayStreakItem,
  MonthlyStatItem,
  NextStudyItem,
  RecommendationItem,
  StudentTodayViewModel,
  SubjectHourBreakdown,
  TodayPlanItem,
  UpcomingEventItem,
  WeeklyProgressData,
} from './types';

export interface StudentTodayViewProps {
  initialData?: StudentTodayViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
  onStartStudy?: (studyTitle: string) => void;
  onRecommendationAction?: (topic: string) => void;
}

type TaskBackendRef =
  | { kind: 'plan'; studentId: string; planId: string; taskId: string }
  | { kind: 'coach'; studentId: string; taskId: string };

const DAY_LABELS_TR = ['Paz', 'Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt'];
const BAR_COLORS = ['bg-primary-500', 'bg-success', 'bg-warning', 'bg-attention', 'bg-info'];

export const StudentTodayView: React.FC<StudentTodayViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
  onStartStudy,
  onRecommendationAction,
}) => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [activeTab, setActiveTab] = useState<'plan' | 'progress'>('plan');
  const [viewData, setViewData] = useState<StudentTodayViewModel | undefined>(initialData);
  const [plans, setPlans] = useState<TodayPlanItem[]>(initialData?.todayPlans || []);
  const [taskRefs, setTaskRefs] = useState<Record<number, TaskBackendRef>>({});
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading);
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const fetchTodayData = useCallback(async () => {
    if (initialData) return;

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const studentId = await academicService.getCurrentStudentId();
      if (!studentId) {
        setViewData(undefined);
        setPlans([]);
        return;
      }

      const [activePlanRes, coachTasksRes, recsRes, student360Res] = await Promise.allSettled([
        academicService.getActiveStudyPlan(studentId),
        coachTaskService.getCoachTasks(studentId),
        academicService.getRecommendations(studentId, { recommendationLimit: 1 }),
        academicService.getStudent360(studentId),
      ]);

      const activePlan = activePlanRes.status === 'fulfilled' ? activePlanRes.value : null;
      const coachTasks =
        coachTasksRes.status === 'fulfilled' && Array.isArray(coachTasksRes.value)
          ? coachTasksRes.value.filter((t) => t.status !== CoachTaskStatus.Cancelled)
          : [];
      const topRec =
        recsRes.status === 'fulfilled' && Array.isArray(recsRes.value) && recsRes.value.length > 0
          ? recsRes.value[0]
          : null;
      const student360 = student360Res.status === 'fulfilled' ? student360Res.value : null;

      const todayIso = new Date().toISOString().split('T')[0];
      const nextRefs: Record<number, TaskBackendRef> = {};
      const todayItems: TodayPlanItem[] = [];
      let itemCounter = 1;
      let assignedActive = false;

      if (activePlan && activePlan.tasks?.length > 0) {
        const hasTodayDateMatch = activePlan.tasks.some(
          (t) => t.plannedDate && t.plannedDate.startsWith(todayIso)
        );
        const firstIncompleteDay =
          activePlan.tasks.find((t) => !t.isCompleted)?.dayIndex ?? activePlan.tasks[0].dayIndex;
        const targetDayTasks = hasTodayDateMatch
          ? activePlan.tasks.filter((t) => t.plannedDate && t.plannedDate.startsWith(todayIso))
          : activePlan.tasks.filter((t) => t.dayIndex === firstIncompleteDay);

        for (const t of targetDayTasks) {
          const isDone = t.isCompleted;
          let status: TodayPlanItem['status'] = 'upcoming';
          if (isDone) {
            status = 'completed';
          } else if (!assignedActive) {
            status = 'active';
            assignedActive = true;
          }

          const numericId = itemCounter++;
          nextRefs[numericId] = {
            kind: 'plan',
            studentId,
            planId: activePlan.plan.id,
            taskId: t.id,
          };

          todayItems.push({
            id: numericId,
            subject: t.subjectName || (t.nodeType === 'QuestionStyle' ? 'Soru Stili' : 'Genel Çalışma'),
            topic: t.titleSnapshot || t.topicName || t.learningOutcomeName || 'Çalışma Görevi',
            durationMinutes: t.durationMinutes,
            status,
            completedAt: t.completedAt
              ? new Date(t.completedAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
              : undefined,
          });
        }
      }

      for (const ct of coachTasks) {
        const isDone = ct.isCompleted || ct.status === CoachTaskStatus.Completed;
        let status: TodayPlanItem['status'] = 'upcoming';
        if (isDone) {
          status = 'completed';
        } else if (!assignedActive) {
          status = 'active';
          assignedActive = true;
        }

        const numericId = itemCounter++;
        nextRefs[numericId] = {
          kind: 'coach',
          studentId,
          taskId: ct.id,
        };

        todayItems.push({
          id: numericId,
          subject: ct.subjectName || 'Koç Görevi',
          topic: ct.title,
          durationMinutes: ct.durationMinutes || 30,
          status,
          completedAt: ct.completedAt
            ? new Date(ct.completedAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
            : undefined,
        });
      }

      const activeCandidate = todayItems.find((i) => i.status !== 'completed') || todayItems[0];
      const nextStudy: NextStudyItem = activeCandidate
        ? {
            title: `${activeCandidate.subject} — ${activeCandidate.topic}`,
            topic: activeCandidate.topic,
            durationMinutes: activeCandidate.durationMinutes,
            subject: activeCandidate.subject,
          }
        : {
            title: 'Günlük Çalışma',
            topic: 'Akademik Tekrar',
            durationMinutes: 30,
            subject: 'Genel',
          };

      let weeklyProgress: WeeklyProgressData | undefined;
      if (activePlan && activePlan.tasks?.length > 0) {
        const totalDays = Math.min(Math.max(activePlan.plan.planDays || 7, 1), 7);
        const startObj = activePlan.plan.startDate ? new Date(activePlan.plan.startDate) : new Date();
        const days: DayStreakItem[] = [];

        for (let d = 1; d <= totalDays; d++) {
          const dTasks = activePlan.tasks.filter((t) => t.dayIndex === d);
          const dObj = new Date(startObj.getTime() + (d - 1) * 86400000);
          const dIso = dObj.toISOString().split('T')[0];
          days.push({
            label: DAY_LABELS_TR[dObj.getDay()] || `${d}.G`,
            done: dTasks.length > 0 && dTasks.every((t) => t.isCompleted),
            today: dIso === todayIso,
          });
        }

        const completedHours = Math.round(((activePlan.plan.completedTaskMinutes || 0) / 60) * 10) / 10;
        const targetHours = Math.round(((activePlan.plan.totalPlannedMinutes || 0) / 60) * 10) / 10;
        const remainingHours = Math.max(0, Math.round((targetHours - completedHours) * 10) / 10);
        const adherencePercentage = Math.round((activePlan.plan.taskProgressRate || 0) * 100);

        const subjMinutes = new Map<string, number>();
        for (const t of activePlan.tasks) {
          const sName = t.subjectName || (t.nodeType === 'QuestionStyle' ? 'Soru Stili' : 'Genel');
          subjMinutes.set(sName, (subjMinutes.get(sName) || 0) + t.durationMinutes);
        }

        const subjects: SubjectHourBreakdown[] = Array.from(subjMinutes.entries()).map(([subject, mins], idx) => ({
          subject,
          hours: Math.round((mins / 60) * 10) / 10,
          colorClass: BAR_COLORS[idx % BAR_COLORS.length],
        }));

        weeklyProgress = {
          days,
          completedHours,
          targetHours,
          remainingHours,
          adherencePercentage,
          subjects,
        };
      }

      const recommendation: RecommendationItem | undefined = topRec
        ? {
            topic: topRec.name,
            message: `${getRecommendationTypeLabel(topRec.recommendationType)}: ${
              topRec.subjectName ? `${topRec.subjectName} — ` : ''
            }${topRec.name} (Öncelik Puanı: ${Math.round(topRec.priorityScore)})`,
            actionLabel: 'Akademik Analizi İncele',
          }
        : undefined;

      const upcomingEvents: UpcomingEventItem[] = [];
      coachTasks
        .filter((ct) => !ct.isCompleted && ct.dueDate)
        .slice(0, 3)
        .forEach((ct, idx) => {
          upcomingEvents.push({
            id: idx + 1,
            date: new Date(ct.dueDate!).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long' }),
            label: ct.title,
            type: 'session',
          });
        });

      const monthlyStats: MonthlyStatItem[] = [];
      if (student360?.recentExam) {
        monthlyStats.push({
          label: 'Son Deneme Neti',
          value: `${student360.recentExam.totalNet}`,
          type: 'exam',
        });
      }
      if (activePlan) {
        monthlyStats.push({
          label: 'Plan Uyumu',
          value: `%${Math.round((activePlan.plan.taskProgressRate || 0) * 100)}`,
          type: 'award',
        });
      }

      setTaskRefs(nextRefs);
      setPlans(todayItems);
      setViewData({
        studentName: student360?.student?.fullName || user?.name || 'Öğrenci',
        dateString: new Date().toLocaleDateString('tr-TR'),
        nextStudy,
        todayPlans: todayItems,
        weeklyProgress: weeklyProgress as WeeklyProgressData,
        recommendation: recommendation as RecommendationItem,
        upcomingEvents,
        monthlyStats,
      });
    } catch {
      setViewData(undefined);
      setPlans([]);
    } finally {
      setIsLoading(false);
    }
  }, [initialData, user?.name]);

  useEffect(() => {
    if (initialData) {
      setViewData(initialData);
      setPlans(initialData.todayPlans || []);
      setIsLoading(false);
    } else {
      fetchTodayData();
    }
  }, [initialData, fetchTodayData]);

  const activeData = viewData || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const studentDisplayName =
    user?.name || activeData?.studentName || user?.email?.split('@')[0] || 'Öğrenci';
  const currentDateFormatted = new Date().toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  });

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="student-today-loading">
        <div className="space-y-2">
          <Skeleton className="h-4 w-32 rounded" />
          <Skeleton className="h-8 w-64 rounded-lg" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="lg:col-span-2 space-y-4">
            <Skeleton className="h-40 w-full rounded-2xl" />
            <Skeleton className="h-56 w-full rounded-2xl" />
          </div>
          <div className="space-y-4">
            <Skeleton className="h-32 w-full rounded-2xl" />
            <Skeleton className="h-48 w-full rounded-2xl" />
          </div>
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4" data-testid="student-today-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Sayfa Yüklenemedi">
          {activeError}
        </Alert>
        <div className="text-center pt-2">
          <Button
            variant="primary"
            size="sm"
            onClick={() => (onRetry ? onRetry() : fetchTodayData())}
            leftIcon={<RefreshCw className="h-4 w-4" />}
          >
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  // Toggle status (persisted to backend when backed by taskRefs, or local when initialData is used)
  const handleTogglePlanStatus = async (id: number) => {
    const target = plans.find((p) => p.id === id);
    if (!target) return;

    const ref = taskRefs[id];
    if (ref && !initialData) {
      const nextCompleted = target.status !== 'completed';
      try {
        if (ref.kind === 'plan') {
          await academicService.updateTaskCompletion(ref.studentId, ref.planId, ref.taskId, nextCompleted);
        } else {
          await coachTaskService.toggleTaskCompletion(ref.studentId, ref.taskId, nextCompleted);
        }
        setPlans((prev) =>
          prev.map((item) =>
            item.id === id
              ? {
                  ...item,
                  status: nextCompleted ? 'completed' : 'active',
                  completedAt: nextCompleted
                    ? new Date().toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
                    : undefined,
                }
              : item
          )
        );
        return;
      } catch {
        // Fall through to local state toggle if API call fails
      }
    }

    setPlans((prev) =>
      prev.map((item) => {
        if (item.id === id) {
          if (item.status === 'completed') {
            return { ...item, status: 'upcoming', completedAt: undefined };
          } else if (item.status === 'active') {
            return {
              ...item,
              status: 'completed',
              completedAt: new Date().toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit',
              }),
            };
          } else {
            return { ...item, status: 'active' };
          }
        }
        return item;
      })
    );
  };

  const handleContinuePlan = (id: number) => {
    const plan = plans.find((p) => p.id === id);
    if (plan) {
      if (onStartStudy) {
        onStartStudy(plan.topic);
      } else {
        navigate('/student/plans');
      }
    }
  };

  const hasPlans = plans.length > 0;
  const completedCount = plans.filter((t) => t.status === 'completed').length;

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="student-today-view">
      {/* 1. Calm Greeting Header */}
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-1 border-b border-neutral-100 pb-3 sm:pb-4">
        <div>
          <p className="text-neutral-500 text-xs sm:text-sm font-medium capitalize">
            {currentDateFormatted}
          </p>
          <h1 className="font-serif text-2xl sm:text-3xl lg:text-4xl text-neutral-900 tracking-tight leading-tight mt-0.5">
            İyi çalışmalar, {studentDisplayName}
          </h1>
        </div>
        {user?.institutionName && (
          <div className="mt-1 sm:mt-0">
            <span className="inline-flex items-center gap-1.5 text-xs text-neutral-600 bg-neutral-100 px-3 py-1 rounded-full border border-neutral-200">
              <span className="w-1.5 h-1.5 rounded-full bg-success" />
              {user.institutionName}
            </span>
          </div>
        )}
      </div>

      {hasPlans ? (
        /* Real Plans Layout (When plans exist) */
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-4">
          <div className="lg:col-span-2 space-y-3.5 sm:space-y-4">
            {activeData?.nextStudy && (
              <NextStudyCard
                nextStudy={activeData.nextStudy}
                completedCount={completedCount}
                totalCount={plans.length}
                onStartStudy={() =>
                  onStartStudy ? onStartStudy(activeData.nextStudy.title) : navigate('/student/plans')
                }
              />
            )}

            <div className="bg-white rounded-2xl border border-neutral-100 overflow-hidden shadow-soft-sm">
              <div className="flex border-b border-neutral-100">
                <button
                  type="button"
                  onClick={() => setActiveTab('plan')}
                  className={cn(
                    'flex-1 py-2.5 sm:py-3.5 text-xs sm:text-sm font-medium transition-colors min-h-[44px]',
                    activeTab === 'plan'
                      ? 'text-primary-600 border-b-2 border-primary-500 font-semibold -mb-px'
                      : 'text-neutral-500 hover:text-neutral-700'
                  )}
                >
                  Bugünkü Plan
                </button>
                {activeData?.weeklyProgress && (
                  <button
                    type="button"
                    onClick={() => setActiveTab('progress')}
                    className={cn(
                      'flex-1 py-2.5 sm:py-3.5 text-xs sm:text-sm font-medium transition-colors min-h-[44px]',
                      activeTab === 'progress'
                        ? 'text-primary-600 border-b-2 border-primary-500 font-semibold -mb-px'
                        : 'text-neutral-500 hover:text-neutral-700'
                    )}
                  >
                    İlerleme
                  </button>
                )}
              </div>

              {activeTab === 'plan' ? (
                <TodayPlanTab
                  plans={plans}
                  onTogglePlanStatus={handleTogglePlanStatus}
                  onContinuePlan={handleContinuePlan}
                />
              ) : activeData?.weeklyProgress ? (
                <WeeklyProgressTab progressData={activeData.weeklyProgress} />
              ) : null}
            </div>
          </div>

          <div className="space-y-3.5 sm:space-y-4">
            {activeData?.recommendation && (
              <RecommendationCard
                recommendation={activeData.recommendation}
                onAction={() =>
                  onRecommendationAction
                    ? onRecommendationAction(activeData.recommendation.topic)
                    : navigate('/student/academic')
                }
              />
            )}
            {activeData?.upcomingEvents && activeData.upcomingEvents.length > 0 && (
              <UpcomingCard events={activeData.upcomingEvents} />
            )}
            {activeData?.monthlyStats && activeData.monthlyStats.length > 0 && (
              <MonthlyStatsCard stats={activeData.monthlyStats} />
            )}
          </div>
        </div>
      ) : (
        /* Honest Calm Academic Shell (When no active plan tasks exist yet) */
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-5">
          {/* Main 2-column Focus Card */}
          <div className="lg:col-span-2 space-y-4">
            {/* Primary "Bugün ne yapmalıyım?" Actionable Guidance */}
            <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 sm:p-6 shadow-soft-sm relative overflow-hidden">
              <div className="flex items-start gap-3.5">
                <div className="w-10 h-10 rounded-xl bg-navy-50 text-navy-800 flex items-center justify-center flex-shrink-0 border border-navy-100 mt-0.5">
                  <CalendarDays className="h-5 w-5 text-primary-600" />
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2">
                    <span className="text-[11px] font-semibold text-primary-700 bg-primary-50 px-2 py-0.5 rounded-md border border-primary-100">
                      GÜNLÜK BAKIŞ
                    </span>
                    <span className="text-xs text-neutral-400">• Bugün ne yapmalıyım?</span>
                  </div>
                  <h2 className="text-lg sm:text-xl font-semibold text-neutral-900 mt-1.5">
                    Bugün İçin Planlanmış Görev Bulunmuyor
                  </h2>
                  <p className="text-xs sm:text-sm text-neutral-600 mt-2 leading-relaxed">
                    Bugün için henüz atanmış bir çalışma programı veya ödev bulunmuyor. Danışman koçunuz veya branş öğretmeniniz haftalık programınızı oluşturduğunda ders ve konu bazlı hedefleriniz burada adım adım yer alacaktır.
                  </p>
                </div>
              </div>

              {/* Action Suggestions */}
              <div className="mt-5 pt-4 border-t border-neutral-100 grid grid-cols-1 sm:grid-cols-2 gap-2.5">
                <button
                  type="button"
                  onClick={() => navigate('/student/academic')}
                  className="flex items-center gap-3 p-3 rounded-xl border border-neutral-100 bg-neutral-50/60 hover:bg-neutral-100/80 hover:border-neutral-200 transition-all text-left group min-h-[52px]"
                >
                  <Sparkles className="h-4 w-4 text-primary-600 flex-shrink-0 group-hover:scale-110 transition-transform" />
                  <div className="min-w-0">
                    <p className="text-xs font-semibold text-neutral-800">Akademik Analiz & Zeka</p>
                    <p className="text-[11px] text-neutral-500 truncate">Başarım ve zayıf konularını incele</p>
                  </div>
                </button>

                <button
                  type="button"
                  onClick={() => navigate('/student/plans')}
                  className="flex items-center gap-3 p-3 rounded-xl border border-neutral-100 bg-neutral-50/60 hover:bg-neutral-100/80 hover:border-neutral-200 transition-all text-left group min-h-[52px]"
                >
                  <BookOpen className="h-4 w-4 text-primary-600 flex-shrink-0 group-hover:scale-110 transition-transform" />
                  <div className="min-w-0">
                    <p className="text-xs font-semibold text-neutral-800">Çalışma Planlarım</p>
                    <p className="text-[11px] text-neutral-500 truncate">Haftalık planını ve hedeflerini gör</p>
                  </div>
                </button>

                <button
                  type="button"
                  onClick={() => navigate('/student/exams')}
                  className="flex items-center gap-3 p-3 rounded-xl border border-neutral-100 bg-neutral-50/60 hover:bg-neutral-100/80 hover:border-neutral-200 transition-all text-left group min-h-[52px]"
                >
                  <FileText className="h-4 w-4 text-primary-600 flex-shrink-0 group-hover:scale-110 transition-transform" />
                  <div className="min-w-0">
                    <p className="text-xs font-semibold text-neutral-800">Deneme Sınavları</p>
                    <p className="text-[11px] text-neutral-500 truncate">Sınav durumunu ve kayıtları incele</p>
                  </div>
                </button>

                <button
                  type="button"
                  onClick={() => navigate('/student/profile')}
                  className="flex items-center gap-3 p-3 rounded-xl border border-neutral-100 bg-neutral-50/60 hover:bg-neutral-100/80 hover:border-neutral-200 transition-all text-left group min-h-[52px]"
                >
                  <User className="h-4 w-4 text-primary-600 flex-shrink-0 group-hover:scale-110 transition-transform" />
                  <div className="min-w-0">
                    <p className="text-xs font-semibold text-neutral-800">Profil & Tercihler</p>
                    <p className="text-[11px] text-neutral-500 truncate">Hesap ve bildirim tercihlerini yönet</p>
                  </div>
                </button>
              </div>
            </div>

            {/* Self-Study Calm Continuation Banner */}
            <div className="bg-surface rounded-2xl border border-border-subtle p-4 sm:p-5 flex items-start gap-3">
              <BookOpen className="h-5 w-5 text-brand-secondary flex-shrink-0 mt-0.5" />
              <div className="text-xs sm:text-sm text-neutral-700 leading-relaxed">
                <span className="font-semibold text-neutral-900 block mb-0.5">Serbest Çalışma Rehberi</span>
                Yeni bir çalışma programı tanımlanana kadar konu tekrarlarınızı yapabilir ve çözemediğiniz soru havuzlarınızı gözden geçirebilirsiniz.
              </div>
            </div>
          </div>

          {/* Right Column: Institutional Status Summary & Academic Recommendations */}
          <div className="space-y-4">
            {activeData?.recommendation && (
              <RecommendationCard
                recommendation={activeData.recommendation}
                onAction={() =>
                  onRecommendationAction
                    ? onRecommendationAction(activeData.recommendation.topic)
                    : navigate('/student/academic')
                }
              />
            )}

            <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 shadow-soft-sm space-y-4">
              <div className="flex items-center gap-2 text-xs font-semibold text-neutral-500 uppercase tracking-wide">
                <Sparkles className="h-3.5 w-3.5 text-primary-600" />
                <span>Akademik Durum</span>
              </div>

              <div className="space-y-3">
                <div className="flex items-center justify-between py-1.5 border-b border-neutral-100">
                  <span className="text-xs text-neutral-500">Kurum</span>
                  <span className="text-xs font-medium text-neutral-800 text-right truncate max-w-[160px]">
                    {user?.institutionName || 'Kayıtlı Kurum'}
                  </span>
                </div>

                <div className="flex items-center justify-between py-1.5">
                  <span className="text-xs text-neutral-500">Rol</span>
                  <span className="text-xs font-medium text-neutral-800">
                    {user?.roleLabel || 'Öğrenci'}
                  </span>
                </div>
              </div>
            </div>

            {activeData?.monthlyStats && activeData.monthlyStats.length > 0 && (
              <MonthlyStatsCard stats={activeData.monthlyStats} />
            )}
          </div>
        </div>
      )}
    </div>
  );
};
