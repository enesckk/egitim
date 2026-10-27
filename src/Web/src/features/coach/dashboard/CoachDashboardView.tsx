import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Users,
  Calendar,
  CheckCircle2,
  AlertTriangle,
  ArrowRight,
  RefreshCw,
  ChevronRight,
  AlertCircle,
  BookOpen,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { useAuth } from '@/auth';
import { academicService, StudentSummaryDto } from '@/services/academicService';
import { coachMeetingService, CoachMeetingDto } from '@/services/coachMeetingService';
import { coachTaskService, CoachTaskDto, CoachTaskStatus } from '@/services/coachTaskService';

interface StudentAttentionItem {
  studentId: string;
  studentName: string;
  gradeLevel?: number | null;
  issue: string;
  severity: 'warning' | 'danger';
  targetTab: 'plan' | 'tasks' | 'overview';
  overdueTaskCount?: number;
  planProgress?: number;
}

export const CoachDashboardView: React.FC = () => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const [students, setStudents] = useState<StudentSummaryDto[]>([]);
  const [todayMeetings, setTodayMeetings] = useState<CoachMeetingDto[]>([]);
  const [attentionStudents, setAttentionStudents] = useState<StudentAttentionItem[]>([]);
  const [pendingTasks, setPendingTasks] = useState<Array<CoachTaskDto & { studentName: string }>>([]);

  const loadDashboardData = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);

      // 1. Fetch assigned students and all meetings concurrently
      const [studentsRes, meetingsRes] = await Promise.all([
        academicService.getStudents({ page: 1, pageSize: 50 }),
        coachMeetingService.listMeetings().catch(() => [] as CoachMeetingDto[]),
      ]);

      const studentList = studentsRes?.items || [];
      setStudents(studentList);

      // 2. Filter today's meetings
      const todayStr = new Date().toISOString().split('T')[0];
      const todays = (meetingsRes || []).filter((m) => {
        if (!m.startTime) return false;
        return m.startTime.startsWith(todayStr) && m.status !== 'cancelled';
      });
      // Sort chronologically
      todays.sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime());
      setTodayMeetings(todays);

      // 3. Inspect bounded student records for overdue tasks or active plan progress (bounded to top 10)
      const inspectSubset = studentList.slice(0, 10);

      const tasksAndPlans = await Promise.allSettled(
        inspectSubset.map(async (student) => {
          const [tasks, activePlan] = await Promise.all([
            coachTaskService.getCoachTasks(student.id).catch(() => [] as CoachTaskDto[]),
            academicService.getActiveStudyPlan(student.id).catch(() => null),
          ]);
          return { student, tasks, activePlan };
        })
      );

      const attentions: StudentAttentionItem[] = [];
      const collectedPendingTasks: Array<CoachTaskDto & { studentName: string }> = [];
      const now = new Date();

      for (const res of tasksAndPlans) {
        if (res.status !== 'fulfilled') continue;
        const { student, tasks, activePlan } = res.value;

        // Pending Coach Tasks
        const activeTasks = (tasks || []).filter((t) => t.status === CoachTaskStatus.Active && !t.isCompleted);
        for (const t of activeTasks) {
          collectedPendingTasks.push({
            ...t,
            studentName: student.fullName,
          });
        }

        // Check for Overdue Tasks
        const overdueTasks = activeTasks.filter((t) => t.dueDate && new Date(t.dueDate) < now);
        if (overdueTasks.length > 0) {
          attentions.push({
            studentId: student.id,
            studentName: student.fullName,
            gradeLevel: student.gradeLevel,
            issue: `${overdueTasks.length} adet tamamlanmamış/süresi geçmiş koçluk görevi bulunuyor.`,
            severity: 'danger',
            targetTab: 'tasks',
            overdueTaskCount: overdueTasks.length,
          });
        } else if (activePlan?.plan && (activePlan.plan.taskProgressRate || 0) < 0.4) {
          const rate = Math.round((activePlan.plan.taskProgressRate || 0) * 100);
          attentions.push({
            studentId: student.id,
            studentName: student.fullName,
            gradeLevel: student.gradeLevel,
            issue: `Aktif çalışma planı tamamlama oranı %${rate} seviyesinde.`,
            severity: 'warning',
            targetTab: 'plan',
            planProgress: rate,
          });
        }
      }

      setAttentionStudents(attentions);
      setPendingTasks(collectedPendingTasks.slice(0, 5));
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Koçluk verileri yüklenirken bir hata oluştu.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadDashboardData();
  }, [loadDashboardData]);

  // Formatted date string in Turkish
  const dateFormatted = new Intl.DateTimeFormat('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date());

  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="coach-dashboard-loading">
        <Skeleton className="h-28 w-full rounded-2xl" />
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Skeleton className="h-20 rounded-2xl" />
          <Skeleton className="h-20 rounded-2xl" />
          <Skeleton className="h-20 rounded-2xl" />
          <Skeleton className="h-20 rounded-2xl" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <Skeleton className="lg:col-span-2 h-72 rounded-2xl" />
          <Skeleton className="h-72 rounded-2xl" />
        </div>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 select-none p-4" data-testid="coach-dashboard-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Genel Bakış Yüklenemedi">
          {errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={loadDashboardData} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const coachFirstName = user?.name ? user.name.split(' ')[0] : 'Koç';

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="coach-dashboard-view">
      {/* 1. Coach Hero Banner */}
      <div className="bg-[#14233E] rounded-3xl p-5 sm:p-7 text-white shadow-soft-sm relative overflow-hidden border border-navy-800">
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1.5">
              <span className="text-xs font-medium text-navy-300 font-mono tracking-tight">{dateFormatted}</span>
              <span className="text-navy-500">•</span>
              <span className="text-xs px-2 py-0.5 rounded-full bg-navy-800/80 text-blue-300 border border-navy-700 font-medium">
                {user?.institutionName || 'Kurum Portalı'}
              </span>
            </div>
            <h1 className="font-serif text-2xl sm:text-3xl font-bold tracking-tight text-white leading-tight">
              İyi günler, {coachFirstName}
            </h1>
            <p className="text-xs sm:text-sm text-navy-200 mt-1 max-w-xl leading-relaxed">
              Atanmış öğrencilerinizin haftalık plan uyumu, görüşme takvimi ve koçluk hedefleri güncel olarak listelenmektedir.
            </p>
          </div>

          <div className="flex items-center gap-2.5 shrink-0 flex-wrap">
            <Button
              variant="outline"
              size="sm"
              onClick={() => navigate('/coach/students')}
              leftIcon={<Users className="h-4 w-4" />}
              className="bg-white/10 hover:bg-white/15 text-white border-white/20 text-xs rounded-xl"
            >
              Öğrencilerim
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={() => navigate('/coach/meetings')}
              leftIcon={<Calendar className="h-4 w-4" />}
              className="bg-[#315FE8] hover:bg-blue-600 text-white text-xs rounded-xl shadow-soft-xs"
            >
              Görüşmeler
            </Button>
          </div>
        </div>
      </div>

      {/* 2. Real Portfolio KPIs */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 sm:gap-4">
        {/* Total Assigned Students */}
        <div className="bg-white p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Atanmış Öğrenci</span>
            <Users className="h-4 w-4 text-primary-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{students.length}</span>
            <span className="text-xs text-neutral-500 font-medium">aktif</span>
          </div>
        </div>

        {/* Today's Meetings */}
        <div className="bg-white p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Bugünkü Görüşme</span>
            <Calendar className="h-4 w-4 text-blue-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{todayMeetings.length}</span>
            <span className="text-xs text-neutral-500 font-medium">oturum</span>
          </div>
        </div>

        {/* Attention Required */}
        <div className="bg-white p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Takip Gerektiren</span>
            <AlertTriangle className="h-4 w-4 text-amber-500" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{attentionStudents.length}</span>
            <span className="text-xs text-neutral-500 font-medium">öğrenci</span>
          </div>
        </div>

        {/* Pending Tasks */}
        <div className="bg-white p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Bekleyen Görev</span>
            <CheckCircle2 className="h-4 w-4 text-emerald-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{pendingTasks.length}</span>
            <span className="text-xs text-neutral-500 font-medium">görev</span>
          </div>
        </div>
      </div>

      {/* 3. Main Dashboard Sections */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 sm:gap-6">
        {/* Left 2 Columns: Attention Alerts + Today's Sessions */}
        <div className="lg:col-span-2 space-y-4 sm:space-y-6">
          {/* Section: Attention Required Students */}
          <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <AlertTriangle className="h-5 w-5 text-amber-500" />
                <h2 className="font-bold text-neutral-900 text-sm sm:text-base">
                  Öncelikli Takip Gerektiren Öğrenciler
                </h2>
                {attentionStudents.length > 0 && (
                  <span className="px-2 py-0.5 text-xs font-bold rounded-full bg-amber-50 text-amber-700 border border-amber-200">
                    {attentionStudents.length}
                  </span>
                )}
              </div>
            </div>

            {attentionStudents.length === 0 ? (
              <div className="py-6 text-center text-neutral-400 text-xs">
                <CheckCircle2 className="h-8 w-8 text-emerald-500 mx-auto mb-2 opacity-80" />
                Tüm öğrencileriniz görev ve çalışma planlarına düzenli uyum sağlamaktadır.
              </div>
            ) : (
              <div className="space-y-3">
                {attentionStudents.map((item) => (
                  <div
                    key={item.studentId}
                    className={`p-3.5 rounded-xl border flex flex-col sm:flex-row sm:items-center justify-between gap-3 ${
                      item.severity === 'danger'
                        ? 'bg-rose-50/40 border-rose-200/80'
                        : 'bg-amber-50/40 border-amber-200/80'
                    }`}
                  >
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="font-bold text-xs sm:text-sm text-neutral-900">
                          {item.studentName}
                        </span>
                        {item.gradeLevel && (
                          <span className="text-[11px] font-medium text-neutral-500">
                            {item.gradeLevel}. Sınıf
                          </span>
                        )}
                        <span
                          className={`text-[10px] font-bold px-2 py-0.5 rounded-full ${
                            item.severity === 'danger'
                              ? 'bg-rose-100 text-rose-800'
                              : 'bg-amber-100 text-amber-800'
                          }`}
                        >
                          {item.severity === 'danger' ? 'Gecikmiş Görev' : 'Plan Takibi'}
                        </span>
                      </div>
                      <p className="text-xs text-neutral-600 mt-1 leading-relaxed">{item.issue}</p>
                    </div>

                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => navigate(`/coach/students/${item.studentId}?tab=${item.targetTab}`)}
                      rightIcon={<ChevronRight className="h-3.5 w-3.5" />}
                      className="shrink-0 text-xs bg-white hover:bg-neutral-50 rounded-xl"
                    >
                      İncele
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Section: Today's Meetings */}
          <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <Calendar className="h-5 w-5 text-primary-600" />
                <h2 className="font-bold text-neutral-900 text-sm sm:text-base">
                  Bugünkü Görüşmeler
                </h2>
                {todayMeetings.length > 0 && (
                  <span className="px-2 py-0.5 text-xs font-bold rounded-full bg-primary-50 text-primary-700 border border-primary-200">
                    {todayMeetings.length}
                  </span>
                )}
              </div>
              <button
                type="button"
                onClick={() => navigate('/coach/meetings')}
                className="text-xs text-primary-600 hover:text-primary-800 font-semibold flex items-center gap-1 cursor-pointer"
              >
                Tüm Takvim <ArrowRight className="h-3.5 w-3.5" />
              </button>
            </div>

            {todayMeetings.length === 0 ? (
              <div className="py-6 text-center text-neutral-400 text-xs">
                Bugün için planlanmış herhangi bir koçluk görüşmesi bulunmuyor.
              </div>
            ) : (
              <div className="space-y-2.5">
                {todayMeetings.map((m) => (
                  <div
                    key={m.id}
                    className="p-3 rounded-xl border border-neutral-200/80 bg-neutral-50/50 flex items-center justify-between gap-3 hover:bg-neutral-50 transition-colors"
                  >
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-xl bg-[#14233E] text-white font-bold text-xs flex items-center justify-center shrink-0">
                        {m.studentName
                          ? m.studentName
                              .split(' ')
                              .map((w) => w[0])
                              .join('')
                              .substring(0, 2)
                              .toUpperCase()
                          : 'ÖĞ'}
                      </div>
                      <div>
                        <div className="flex items-center gap-2">
                          <span className="font-bold text-xs sm:text-sm text-neutral-900">
                            {m.studentName}
                          </span>
                          <span className="text-[11px] text-neutral-500 font-mono">
                            {new Date(m.startTime).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                          </span>
                        </div>
                        <p className="text-xs text-neutral-500">{m.type || 'Haftalık Değerlendirme'}</p>
                      </div>
                    </div>

                    <Button
                      variant="primary"
                      size="sm"
                      onClick={() => navigate('/coach/meetings')}
                      className="shrink-0 text-xs bg-[#315FE8] rounded-xl"
                    >
                      Detay
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Right 1 Column: Pending Tasks & Quick Actions */}
        <div className="space-y-4 sm:space-y-6">
          {/* Pending Coach Tasks */}
          <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs">
            <div className="flex items-center justify-between mb-4">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="h-5 w-5 text-emerald-600" />
                <h2 className="font-bold text-neutral-900 text-sm">Bekleyen Görevler</h2>
              </div>
            </div>

            {pendingTasks.length === 0 ? (
              <div className="py-6 text-center text-neutral-400 text-xs">
                Öğrencilerinize atanmış bekleyen aktif görev bulunmuyor.
              </div>
            ) : (
              <div className="space-y-2.5">
                {pendingTasks.map((task) => (
                  <div
                    key={task.id}
                    onClick={() => navigate(`/coach/students/${task.studentId}?tab=tasks`)}
                    className="p-3 rounded-xl border border-neutral-100 bg-neutral-50/60 hover:bg-neutral-100/60 transition-colors cursor-pointer"
                  >
                    <div className="flex items-start justify-between gap-2">
                      <h4 className="text-xs font-bold text-neutral-900 line-clamp-1">{task.title}</h4>
                      {task.dueDate && (
                        <span className="text-[10px] text-neutral-400 font-mono shrink-0">
                          {new Date(task.dueDate).toLocaleDateString([], { month: 'short', day: 'numeric' })}
                        </span>
                      )}
                    </div>
                    <div className="flex items-center justify-between mt-1 text-[11px] text-neutral-500">
                      <span>{task.studentName}</span>
                      {task.subjectName && <span className="text-primary-700 font-medium">{task.subjectName}</span>}
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Quick Portfolio Actions */}
          <div className="bg-[#F8F7F3] rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs">
            <h3 className="font-bold text-neutral-900 text-xs uppercase tracking-wider mb-3">
              Hızlı İşlemler
            </h3>
            <div className="space-y-2">
              <button
                type="button"
                onClick={() => navigate('/coach/students')}
                className="w-full text-left p-3 rounded-xl bg-white border border-neutral-200/80 hover:border-primary-300 hover:shadow-soft-xs transition-all flex items-center justify-between text-xs font-bold text-neutral-800 cursor-pointer"
              >
                <div className="flex items-center gap-2.5">
                  <Users className="h-4 w-4 text-primary-600" />
                  <span>Öğrenci Portföyü</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-400" />
              </button>

              <button
                type="button"
                onClick={() => navigate('/coach/meetings')}
                className="w-full text-left p-3 rounded-xl bg-white border border-neutral-200/80 hover:border-primary-300 hover:shadow-soft-xs transition-all flex items-center justify-between text-xs font-bold text-neutral-800 cursor-pointer"
              >
                <div className="flex items-center gap-2.5">
                  <Calendar className="h-4 w-4 text-primary-600" />
                  <span>Yeni Görüşme Planla</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-400" />
              </button>

              <button
                type="button"
                onClick={() => navigate('/coach/messages')}
                className="w-full text-left p-3 rounded-xl bg-white border border-neutral-200/80 hover:border-primary-300 hover:shadow-soft-xs transition-all flex items-center justify-between text-xs font-bold text-neutral-800 cursor-pointer"
              >
                <div className="flex items-center gap-2.5">
                  <BookOpen className="h-4 w-4 text-primary-600" />
                  <span>Öğrenci & Veli Mesajları</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-400" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default CoachDashboardView;
