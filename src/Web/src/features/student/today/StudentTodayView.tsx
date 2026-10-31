import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  CalendarDays,
  FileText,
  Sparkles,
  BookMarked,
  FolderArchive,
  BarChart3,
  Play,
  TrendingUp,
  MessageSquare,
  AlertCircle,
  RefreshCw,
  ChevronRight,
  BookOpen
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/auth';
import { isCanceledError } from '@/services/api/apiErrors';
import { academicService } from '@/services/academicService';
import { coachTaskService, CoachTaskStatus } from '@/services/coachTaskService';
import type { CoachMeetingDto } from '@/services/coachMeetingService';
import { examService } from '@/services/examService';
import { getRecommendationTypeLabel } from '@/features/student/academic/types';
import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import { Skeleton } from '@/components/ui/Skeleton';
import { StudentTodayViewModel, TodayPlanItem } from './types';

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

// Helper to normalize topic/title avoiding "Matematik — Matematik - Fonksiyonlar" duplication
function cleanTopicTitle(subject: string, rawTitle: string): string {
  if (!rawTitle) return 'Çalışma Görevi';
  let cleaned = rawTitle.trim();
  // Strip leading subject repetition if present (e.g. "Matematik - " or "Matematik — ")
  const prefixRegex = new RegExp(`^${subject}\\s*[-—–:]\\s*`, 'i');
  if (prefixRegex.test(cleaned)) {
    cleaned = cleaned.replace(prefixRegex, '').trim();
  }
  return cleaned || rawTitle;
}

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

  const [viewData, setViewData] = useState<StudentTodayViewModel | undefined>(initialData);
  const [plans, setPlans] = useState<TodayPlanItem[]>(initialData?.todayPlans || []);
  const [, setTaskRefs] = useState<Record<number, TaskBackendRef>>({});
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading);
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  // Additional Real State for Exam & Coach Meeting
  const [latestExam, setLatestExam] = useState<{ title: string; date: string; net: number; diff?: number } | null>(null);
  const [upcomingMeeting, setUpcomingMeeting] = useState<CoachMeetingDto | null>(null);

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

      const [activePlanRes, coachTasksRes, recsRes, student360Res, examsRes] = await Promise.allSettled([
        academicService.getActiveStudyPlan(studentId),
        coachTaskService.getCoachTasks(studentId),
        academicService.getRecommendations(studentId, { recommendationLimit: 1 }),
        academicService.getStudent360(studentId),
        examService.getStudentExamResults(studentId, { pageSize: 5 }),
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

      // Note: Backend /api/v1/coaching/meetings is coach/admin scoped.
      // Student-authorized meeting endpoint is not present in the backend; skip to avoid 403 Forbidden.
      setUpcomingMeeting(null);

      // Real Exams (compare latest with previous if available)
      if (examsRes.status === 'fulfilled' && examsRes.value?.items?.length) {
        const items = examsRes.value.items;
        const latest = items[0];
        let diff: number | undefined = undefined;
        if (items.length > 1) {
          diff = Math.round((latest.totalNet - items[1].totalNet) * 10) / 10;
        }
        setLatestExam({
          title: latest.examTitle || 'Deneme Sınavı',
          date: latest.takenAt ? new Date(latest.takenAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric' }) : 'Son Sınav',
          net: Math.round(latest.totalNet * 10) / 10,
          diff,
        });
      } else if (student360?.recentExam) {
        setLatestExam({
          title: 'Son Deneme Sınavı',
          date: 'Son Sınav',
          net: Math.round(student360.recentExam.totalNet * 10) / 10,
        });
      }

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

          const subject = t.subjectName || (t.nodeType === 'QuestionStyle' ? 'Soru Stili' : 'Genel');
          const rawTopic = t.titleSnapshot || t.topicName || t.learningOutcomeName || 'Çalışma Görevi';

          todayItems.push({
            id: numericId,
            subject,
            topic: cleanTopicTitle(subject, rawTopic),
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

        const subject = ct.subjectName || 'Koç Görevi';

        todayItems.push({
          id: numericId,
          subject,
          topic: cleanTopicTitle(subject, ct.title),
          durationMinutes: ct.durationMinutes || 30,
          status,
          completedAt: ct.completedAt
            ? new Date(ct.completedAt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' })
            : undefined,
        });
      }

      const activeCandidate = todayItems.find((i) => i.status !== 'completed') || todayItems[0];
      const nextStudy = activeCandidate
        ? {
            title: `${activeCandidate.subject} — ${activeCandidate.topic}`,
            topic: activeCandidate.topic,
            durationMinutes: activeCandidate.durationMinutes,
            subject: activeCandidate.subject,
          }
        : null;

      const recommendation = topRec
        ? {
            topic: topRec.name,
            subject: topRec.subjectName || 'Matematik',
            priorityScore: Math.round(topRec.priorityScore),
            message: `${getRecommendationTypeLabel(topRec.recommendationType)}: Son denemelerde bu konuda daha fazla çalışmaya ihtiyaç var.`,
          }
        : null;

      setTaskRefs(nextRefs);
      setPlans(todayItems);
      setViewData({
        studentName: student360?.student?.fullName || user?.name || 'Ahmet Yılmaz',
        dateString: new Date().toLocaleDateString('tr-TR'),
        nextStudy: nextStudy || undefined,
        todayPlans: todayItems,
        recommendation: recommendation as any,
        upcomingEvents: [],
        monthlyStats: [],
      });
    } catch (err: unknown) {
      if (isCanceledError(err)) return;
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

  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const handleStartNextStudy = () => {
    if (onStartStudy) {
      onStartStudy(activeCandidate?.topic || 'Çalışma');
    } else {
      navigate('/student/plans');
    }
  };

  const completedCount = plans.filter((t) => t.status === 'completed').length;
  const totalCount = plans.length;
  const activeCandidate = plans.find((i) => i.status !== 'completed') || plans[0];

  // Exact 50% for 1/2 calculation
  const progressPercent = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none animate-pulse" data-testid="student-today-loading">
        <Skeleton className="h-44 w-full rounded-3xl bg-neutral-200" />
        <Skeleton className="h-28 w-full rounded-2xl bg-neutral-200" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-40 w-full rounded-2xl bg-neutral-200" />
          <Skeleton className="h-40 w-full rounded-2xl bg-neutral-200" />
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

  return (
    <div className="max-w-5xl mx-auto space-y-4 sm:space-y-5 select-none" data-testid="student-today-view">
      {/* 2. TODAY'S STUDY PROGRAM (Navy Hero Card) */}
      <div className="bg-[#14233E] rounded-3xl p-5 sm:p-7 text-white shadow-[0_12px_32px_rgba(20,35,62,0.18)] relative overflow-hidden">
        {/* Subtle Decorative Icon in background */}
        <div className="absolute right-4 bottom-2 opacity-15 pointer-events-none hidden sm:block">
          <BookOpen className="w-32 h-32 text-blue-200" />
        </div>

        <div className="relative z-10">
          <div className="flex items-center justify-between mb-3 sm:mb-4">
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-white/10 backdrop-blur-md text-[11px] sm:text-xs font-semibold text-blue-100 tracking-wide">
              <CalendarDays className="w-3.5 h-3.5" />
              Bugünün Programı
            </span>
            {totalCount > 0 && (
              <span className="text-xs font-medium text-blue-200">
                {completedCount}/{totalCount} tamamlandı
              </span>
            )}
          </div>

          {activeCandidate ? (
            <>
              <h2 className="text-xl sm:text-2xl font-bold tracking-tight text-white mb-1.5 leading-snug">
                {activeCandidate.subject} — {activeCandidate.topic}
              </h2>
              <p className="text-xs sm:text-sm text-blue-200/90 mb-5 leading-normal">
                Konu tekrarı ve soru çözümü • {activeCandidate.durationMinutes} dk
              </p>

              <button
                type="button"
                onClick={handleStartNextStudy}
                className="inline-flex items-center gap-2 px-7 py-2.5 rounded-full bg-[#315FE8] hover:bg-[#254fc9] active:scale-95 text-white text-xs sm:text-sm font-semibold shadow-md transition-all min-h-[44px]"
              >
                <Play className="w-3.5 h-3.5 fill-current" />
                Başla
              </button>
            </>
          ) : (
            <div className="py-2">
              <h2 className="text-lg sm:text-xl font-bold text-white mb-1">
                Bugün için planlanmış çalışma bulunmuyor
              </h2>
              <p className="text-xs text-blue-200/80 mb-4">
                Yeni hedefler belirlemek veya serbest çalışma oluşturmak için planlarını inceleyebilirsin.
              </p>
              <button
                type="button"
                onClick={() => navigate('/student/plans')}
                className="inline-flex items-center gap-2 px-5 py-2 rounded-full bg-white/15 hover:bg-white/20 text-white text-xs font-semibold transition-all min-h-[40px]"
              >
                Planları Görüntüle
              </button>
            </div>
          )}
        </div>
      </div>

      {/* 3. QUICK ACCESS (One White Rounded Container with 4 Evenly Spaced Shortcuts) */}
      <div className="bg-white rounded-2xl p-4 sm:p-5 shadow-xs border border-neutral-200/70">
        <div className="grid grid-cols-4 gap-2 sm:gap-4 text-center">
          <button
            type="button"
            onClick={() => navigate('/student/plans')}
            className="flex flex-col items-center group cursor-pointer focus:outline-none"
          >
            <div className="w-12 h-12 rounded-2xl bg-blue-50 text-[#315FE8] flex items-center justify-center group-hover:scale-105 group-hover:bg-blue-100/70 transition-all">
              <CalendarDays className="w-5 h-5" />
            </div>
            <span className="text-xs font-semibold text-neutral-800 mt-2 leading-none">Planım</span>
            <span className="text-[10px] text-neutral-400 mt-0.5 hidden sm:inline">Çalışma planı</span>
          </button>

          <button
            type="button"
            onClick={() => navigate('/student/notebook')}
            className="flex flex-col items-center group cursor-pointer focus:outline-none"
          >
            <div className="w-12 h-12 rounded-2xl bg-amber-50 text-amber-600 flex items-center justify-center group-hover:scale-105 group-hover:bg-amber-100/70 transition-all">
              <BookMarked className="w-5 h-5" />
            </div>
            <span className="text-xs font-semibold text-neutral-800 mt-2 leading-none">Defterim</span>
            <span className="text-[10px] text-neutral-400 mt-0.5 hidden sm:inline">Notlarını düzenle</span>
          </button>

          <button
            type="button"
            onClick={() => navigate('/student/materials')}
            className="flex flex-col items-center group cursor-pointer focus:outline-none"
          >
            <div className="w-12 h-12 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center group-hover:scale-105 group-hover:bg-indigo-100/70 transition-all">
              <FolderArchive className="w-5 h-5" />
            </div>
            <span className="text-xs font-semibold text-neutral-800 mt-2 leading-none">Materyaller</span>
            <span className="text-[10px] text-neutral-400 mt-0.5 hidden sm:inline">Ders kaynakları</span>
          </button>

          <button
            type="button"
            onClick={() => navigate('/student/academic')}
            className="flex flex-col items-center group cursor-pointer focus:outline-none"
          >
            <div className="w-12 h-12 rounded-2xl bg-purple-50 text-purple-600 flex items-center justify-center group-hover:scale-105 group-hover:bg-purple-100/70 transition-all">
              <BarChart3 className="w-5 h-5" />
            </div>
            <span className="text-xs font-semibold text-neutral-800 mt-2 leading-none">Gelişim</span>
            <span className="text-[10px] text-neutral-400 mt-0.5 hidden sm:inline">Akademik durum</span>
          </button>
        </div>
      </div>

      {/* Balanced 2-Column Responsive Grid on Desktop (lg:grid-cols-2) */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-5">
        {/* Left Column: 4. DAILY PROGRESS SUMMARY (NO DUPLICATE TASK LIST) */}
        <div className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-sm font-bold text-[#14233E]">Günlük İlerleme</h3>
              <span className="text-xs font-bold text-[#315FE8] bg-blue-50 px-2.5 py-0.5 rounded-full">
                {completedCount}/{totalCount}
              </span>
            </div>

            {/* Clean Progress Bar: exactly 50% for 1/2 */}
            <div className="w-full h-2.5 rounded-full bg-neutral-100 overflow-hidden mb-3">
              <div
                className="h-full bg-[#315FE8] rounded-full transition-all duration-500"
                style={{ width: `${progressPercent}%` }}
              />
            </div>

            <div className="flex items-center justify-between text-xs text-neutral-600 font-medium">
              <span>{completedCount} tamamlandı</span>
              <span>{Math.max(0, totalCount - completedCount)} kalan</span>
            </div>
          </div>

          <div className="mt-4 pt-4 border-t border-neutral-100 flex items-center justify-between text-[11px] text-neutral-500">
            <span>Bugünkü çalışma hedeflerinin %{progressPercent}'ini tamamladın.</span>
            <button
              type="button"
              onClick={() => navigate('/student/plans')}
              className="text-[#315FE8] font-semibold hover:underline inline-flex items-center gap-0.5"
            >
              Plan Detayı <ChevronRight className="w-3 h-3" />
            </button>
          </div>
        </div>

        {/* Right Column: 5. ACADEMIC FOCUS (Akademik Odağım) */}
        {viewData?.recommendation && (
          <div
            onClick={() => {
              if (viewData?.recommendation) {
                if (onRecommendationAction) {
                  onRecommendationAction(viewData.recommendation.topic);
                } else {
                  navigate('/student/academic');
                }
              }
            }}
            className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 hover:border-purple-300 cursor-pointer transition-all group flex flex-col justify-between"
          >
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-[10px] font-bold text-purple-600 uppercase tracking-wider bg-purple-50 px-2 py-0.5 rounded-md">
                  Akademik Odağım
                </span>
                <ChevronRight className="w-4 h-4 text-neutral-400 group-hover:translate-x-0.5 transition-transform" />
              </div>

              <div className="flex items-start gap-3 mt-1">
                <div className="w-10 h-10 rounded-xl bg-purple-50 text-purple-600 flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform mt-0.5">
                  <Sparkles className="w-5 h-5" />
                </div>
                <div className="min-w-0 flex-1">
                  <h4 className="text-sm font-bold text-[#14233E] group-hover:text-purple-700 transition-colors leading-snug">
                    {viewData.recommendation.subject || 'Matematik'} — {viewData.recommendation.topic}
                  </h4>
                  <p className="text-xs text-neutral-500 mt-1 leading-relaxed line-clamp-2">
                    {viewData.recommendation.message}
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-4 pt-3 border-t border-neutral-100 flex items-center justify-between text-[11px] text-neutral-400">
              <span>Sınav netlerini artırmak için öncelikli tekrar önerisi</span>
              <span className="text-purple-600 font-semibold group-hover:underline">Analizi Gör</span>
            </div>
          </div>
        )}

        {/* 6. LATEST EXAM RESULTS (Son Denemem - Only show when real exam data exists) */}
        {latestExam && (
          <div
            onClick={() => navigate('/student/exams')}
            className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 hover:border-blue-300 cursor-pointer transition-all group flex flex-col justify-between"
          >
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-[10px] font-bold text-blue-600 uppercase tracking-wider bg-blue-50 px-2 py-0.5 rounded-md">
                  Son Deneme
                </span>
                <ChevronRight className="w-4 h-4 text-neutral-400 group-hover:translate-x-0.5 transition-transform" />
              </div>

              <div className="flex items-center justify-between mt-1">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 rounded-xl bg-blue-50 text-blue-600 flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform">
                    <FileText className="w-5 h-5" />
                  </div>
                  <div>
                    <h4 className="text-sm font-bold text-[#14233E] group-hover:text-blue-700 transition-colors">
                      {latestExam.title}
                    </h4>
                    <p className="text-xs text-neutral-400 mt-0.5">
                      {latestExam.date}
                    </p>
                  </div>
                </div>

                <div className="text-right">
                  <span className="text-lg sm:text-xl font-bold text-[#14233E]">
                    {latestExam.net} <span className="text-xs font-normal text-neutral-500">net</span>
                  </span>
                  {latestExam.diff !== undefined && (
                    <p className={cn('text-xs font-semibold flex items-center justify-end gap-0.5 mt-0.5', latestExam.diff >= 0 ? 'text-emerald-600' : 'text-red-500')}>
                      <TrendingUp className="w-3.5 h-3.5" />
                      {latestExam.diff >= 0 ? `+${latestExam.diff}` : latestExam.diff}
                    </p>
                  )}
                </div>
              </div>
            </div>

            <div className="mt-4 pt-3 border-t border-neutral-100 flex items-center justify-between text-[11px] text-neutral-400">
              <span>Sınav karnesi ve konu analizi</span>
              <span className="text-blue-600 font-semibold group-hover:underline">Sonuçları İncele</span>
            </div>
          </div>
        )}

        {/* 7. COACH UPDATES (Koçumdan - Only show when real upcoming meeting exists) */}
        {upcomingMeeting && (
          <div
            onClick={() => navigate('/student/messages')}
            className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 hover:border-emerald-300 cursor-pointer transition-all group flex flex-col justify-between"
          >
            <div>
              <div className="flex items-center justify-between mb-2">
                <span className="text-[10px] font-bold text-emerald-600 uppercase tracking-wider bg-emerald-50 px-2 py-0.5 rounded-md">
                  Koçumdan
                </span>
                <ChevronRight className="w-4 h-4 text-neutral-400 group-hover:translate-x-0.5 transition-transform" />
              </div>

              <div className="flex items-center gap-3 mt-1">
                <div className="w-10 h-10 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform">
                  <MessageSquare className="w-5 h-5" />
                </div>
                <div className="min-w-0">
                  <h4 className="text-sm font-bold text-[#14233E] group-hover:text-emerald-700 transition-colors">
                    Görüşme: {upcomingMeeting.type || 'Gelişim Değerlendirmesi'}
                  </h4>
                  <p className="text-xs text-neutral-500 mt-0.5">
                    {upcomingMeeting.dateStr} saat {upcomingMeeting.timeStr} • {upcomingMeeting.locationType}
                  </p>
                </div>
              </div>
            </div>

            <div className="mt-4 pt-3 border-t border-neutral-100 flex items-center justify-between text-[11px] text-neutral-400">
              <span>Koçuna mesaj göndermek için dokun</span>
              <span className="text-emerald-600 font-semibold group-hover:underline">Mesajlaş</span>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};
