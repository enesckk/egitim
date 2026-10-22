import React, { useState, useEffect, useCallback } from 'react';
import {
  Calendar,
  Clock,
  RefreshCw,
  Sparkles,
  CheckCircle2,
  Circle,
  AlertCircle,
  PlayCircle,
  History,
  XCircle,
  Check,
} from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { Alert } from '@/components/ui/Alert';
import { Progress } from '@/components/ui/Progress';
import {
  academicService,
  AcademicQueryParams,
} from '@/services/academicService';
import {
  AcademicStudyPlanDto,
  PersistentStudyPlanDto,
  PersistentStudyPlanDetailDto,
  getClassificationLabel,
  getClassificationBadgeVariant,
} from '../types';

interface AcademicStudyPlanTabProps {
  studentId: string | null;
  studyPlan: AcademicStudyPlanDto | null;
  planDays: number;
  dailyStudyMinutes: number;
  onChangePlanDays: (days: number) => void;
  onChangeDailyStudyMinutes: (minutes: number) => void;
  onRefreshPlan: () => void;
  isLoading?: boolean;
}

export const AcademicStudyPlanTab: React.FC<AcademicStudyPlanTabProps> = ({
  studentId,
  studyPlan,
  planDays,
  dailyStudyMinutes,
  onChangePlanDays,
  onChangeDailyStudyMinutes,
  onRefreshPlan,
  isLoading = false,
}) => {
  const [mode, setMode] = useState<'proposal' | 'active' | 'history'>('proposal');

  // Proposal states
  const [selectedProposalDayIndex, setSelectedProposalDayIndex] = useState<number>(1);
  const [isAccepting, setIsAccepting] = useState<boolean>(false);
  const [acceptConflictError, setAcceptConflictError] = useState<string | null>(null);
  const [acceptValidationError, setAcceptValidationError] = useState<string | null>(null);

  // Active Plan states
  const [activePlanDetail, setActivePlanDetail] = useState<PersistentStudyPlanDetailDto | null>(null);
  const [isLoadingActivePlan, setIsLoadingActivePlan] = useState<boolean>(false);
  const [selectedActiveDayIndex, setSelectedActiveDayIndex] = useState<number>(1);
  const [pendingTaskId, setPendingTaskId] = useState<string | null>(null);
  const [isCancelling, setIsCancelling] = useState<boolean>(false);
  const [showCancelConfirm, setShowCancelConfirm] = useState<boolean>(false);

  // History states
  const [historyPlans, setHistoryPlans] = useState<PersistentStudyPlanDto[]>([]);
  const [isLoadingHistory, setIsLoadingHistory] = useState<boolean>(false);
  const [selectedHistoryPlanDetail, setSelectedHistoryPlanDetail] = useState<PersistentStudyPlanDetailDto | null>(null);

  // Fetch Active Plan
  const fetchActivePlan = useCallback(async () => {
    if (!studentId) return;
    setIsLoadingActivePlan(true);
    try {
      const active = await academicService.getActiveStudyPlan(studentId);
      setActivePlanDetail(active);
    } catch {
      setActivePlanDetail(null);
    } finally {
      setIsLoadingActivePlan(false);
    }
  }, [studentId]);

  // Fetch Plan History
  const fetchHistory = useCallback(async () => {
    if (!studentId) return;
    setIsLoadingHistory(true);
    try {
      const list = await academicService.listStudyPlans(studentId);
      setHistoryPlans(list);
    } catch {
      setHistoryPlans([]);
    } finally {
      setIsLoadingHistory(false);
    }
  }, [studentId]);

  useEffect(() => {
    if (studentId) {
      fetchActivePlan();
    }
  }, [studentId, fetchActivePlan]);

  useEffect(() => {
    if (mode === 'active' && studentId) {
      fetchActivePlan();
    } else if (mode === 'history' && studentId) {
      fetchHistory();
    }
  }, [mode, studentId, fetchActivePlan, fetchHistory]);

  // Accept Proposal Handler ("Planı Uygula")
  const handleAcceptProposal = async () => {
    if (!studentId || isAccepting) return;
    setIsAccepting(true);
    setAcceptConflictError(null);
    setAcceptValidationError(null);

    try {
      const params: AcademicQueryParams = {
        planDays,
        dailyStudyMinutes,
        limit: 50,
        recommendationLimit: 10,
      };
      const result = await academicService.acceptStudyPlan(studentId, params);
      setActivePlanDetail(result);
      setMode('active');
    } catch (err: unknown) {
      const errObj = err as { status?: number; data?: { detail?: string }; message?: string };
      if (errObj?.status === 409 || errObj?.data?.detail?.includes('active study plan')) {
        setAcceptConflictError('Zaten aktif bir çalışma planınız bulunuyor. Tamamlamadan veya iptal etmeden yeni bir plan başlatamazsınız.');
      } else if (errObj?.status === 400 || errObj?.data?.detail?.includes('empty')) {
        setAcceptValidationError('Boş çalışma planı teklifi kabul edilemez. Lütfen süre ve sınav ayarlarını kontrol edin.');
      } else {
        setAcceptValidationError(errObj?.data?.detail || errObj?.message || 'Plan uygulanırken bir hata oluştu.');
      }
    } finally {
      setIsAccepting(false);
    }
  };

  // Toggle Task Completion
  const handleToggleTaskCompletion = async (taskId: string, currentCompleted: boolean) => {
    if (!studentId || !activePlanDetail || pendingTaskId) return;

    // Check terminal plan status
    if (activePlanDetail.plan.status !== 'Active') return;

    setPendingTaskId(taskId);
    try {
      await academicService.updateTaskCompletion(
        studentId,
        activePlanDetail.plan.id,
        taskId,
        !currentCompleted
      );

      // Re-fetch active plan to get updated progress rates & plan status
      await fetchActivePlan();
    } catch (err: unknown) {
      console.error('Task completion update failed:', err);
    } finally {
      setPendingTaskId(null);
    }
  };

  // Cancel Active Plan Handler
  const handleCancelPlan = async () => {
    if (!studentId || !activePlanDetail || isCancelling) return;
    setIsCancelling(true);
    try {
      await academicService.cancelStudyPlan(studentId, activePlanDetail.plan.id);
      setShowCancelConfirm(false);
      await fetchActivePlan();
    } catch (err: unknown) {
      console.error('Plan cancellation failed:', err);
    } finally {
      setIsCancelling(false);
    }
  };

  // Inspect Historical Plan
  const handleSelectHistoryPlan = async (planId: string) => {
    if (!studentId) return;
    try {
      const detail = await academicService.getStudyPlanDetail(studentId, planId);
      setSelectedHistoryPlanDetail(detail);
    } catch {
      setSelectedHistoryPlanDetail(null);
    }
  };

  const selectedProposalDay = studyPlan?.days.find((d) => d.dayIndex === selectedProposalDayIndex) || studyPlan?.days[0];

  return (
    <div className="space-y-4 sm:space-y-6">
      {/* Mode Sub-Navigation Bar */}
      <div className="flex items-center gap-2 bg-neutral-100 p-1 rounded-2xl w-fit">
        <button
          type="button"
          onClick={() => setMode('proposal')}
          className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold transition-all ${
            mode === 'proposal'
              ? 'bg-white text-neutral-900 shadow-sm'
              : 'text-neutral-600 hover:text-neutral-900'
          }`}
        >
          <Sparkles className="h-4 w-4 text-indigo-600" />
          <span>Önerilen Plan (Teklif)</span>
        </button>

        <button
          type="button"
          onClick={() => setMode('active')}
          className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold transition-all ${
            mode === 'active'
              ? 'bg-white text-neutral-900 shadow-sm'
              : 'text-neutral-600 hover:text-neutral-900'
          }`}
        >
          <PlayCircle className="h-4 w-4 text-primary-600" />
          <span>Aktif Planım</span>
          {activePlanDetail && (
            <Badge variant="success" className="text-[10px] ml-1">
              %{Math.round((activePlanDetail.plan.taskProgressRate || 0) * 100)}
            </Badge>
          )}
        </button>

        <button
          type="button"
          onClick={() => setMode('history')}
          className={`flex items-center gap-2 px-4 py-2 rounded-xl text-xs font-semibold transition-all ${
            mode === 'history'
              ? 'bg-white text-neutral-900 shadow-sm'
              : 'text-neutral-600 hover:text-neutral-900'
          }`}
        >
          <History className="h-4 w-4 text-neutral-500" />
          <span>Plan Geçmişi</span>
        </button>
      </div>

      {/* Mode 1: PROPOSAL (Teklif) */}
      {mode === 'proposal' && (
        <div className="space-y-4 sm:space-y-6">
          {/* Conflict Alert */}
          {acceptConflictError && (
            <Alert variant="warning" icon={<AlertCircle className="h-5 w-5" />} title="Aktif Plan Mevcut">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <p className="text-xs text-amber-800">{acceptConflictError}</p>
                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => setMode('active')}
                  className="shrink-0 text-xs py-1"
                >
                  Aktif Plana Git
                </Button>
              </div>
            </Alert>
          )}

          {/* Validation Alert */}
          {acceptValidationError && (
            <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Plan Uygulanamadı">
              <p className="text-xs text-red-800">{acceptValidationError}</p>
            </Alert>
          )}

          {!studyPlan ? (
            <Card className="p-8 text-center bg-white border border-neutral-100">
              <Calendar className="h-10 w-10 text-neutral-300 mx-auto mb-3" />
              <h3 className="text-sm font-semibold text-neutral-900">Plan Teklifi Oluşturulamadı</h3>
              <p className="text-xs text-neutral-500 mt-1">
                Lütfen kapasite ayarlarını kontrol edip tekrar deneyin.
              </p>
            </Card>
          ) : (
            <>
              {/* Proposal Banner */}
              <div className="bg-indigo-50/70 border border-indigo-100 p-4 sm:p-5 rounded-2xl flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                <div className="flex items-start gap-3">
                  <div className="w-10 h-10 rounded-2xl bg-indigo-600 text-white flex items-center justify-center shrink-0 shadow-sm">
                    <Sparkles className="h-5 w-5" />
                  </div>
                  <div>
                    <div className="flex items-center gap-2">
                      <h3 className="font-bold text-neutral-900 text-base">
                        Önerilen Çalışma Planı (Teklif)
                      </h3>
                      <Badge variant="info" className="text-[10px]">
                        Versiyon: {studyPlan.studyPlanPolicyVersion}
                      </Badge>
                    </div>
                    <p className="text-xs text-neutral-600 mt-0.5">
                      Bu plan, akademik önceliklerinize göre türetilmiş dengeli bir taslaktır. Uygulamak için "Planı Uygula" butonuna tıklayın.
                    </p>
                  </div>
                </div>

                {/* Apply & Dynamic Controls */}
                <div className="flex items-center gap-2 self-end sm:self-auto">
                  <div className="flex items-center gap-1.5 bg-white p-1.5 rounded-xl border border-indigo-100 shadow-soft-sm">
                    <Select
                      options={[
                        { value: '3', label: '3 Gün' },
                        { value: '7', label: '7 Gün' },
                        { value: '14', label: '14 Gün' },
                      ]}
                      value={planDays.toString()}
                      onChange={(e) => onChangePlanDays(Number(e.target.value))}
                      className="text-xs py-1 min-h-[32px] w-24"
                    />

                    <Select
                      options={[
                        { value: '60', label: '60 dk/gün' },
                        { value: '90', label: '90 dk/gün' },
                        { value: '120', label: '120 dk/gün' },
                        { value: '180', label: '180 dk/gün' },
                      ]}
                      value={dailyStudyMinutes.toString()}
                      onChange={(e) => onChangeDailyStudyMinutes(Number(e.target.value))}
                      className="text-xs py-1 min-h-[32px] w-28"
                    />

                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={onRefreshPlan}
                      disabled={isLoading}
                      leftIcon={<RefreshCw className={`h-3.5 w-3.5 ${isLoading ? 'animate-spin' : ''}`} />}
                      className="text-xs min-h-[32px] py-1 px-3"
                    >
                      Güncelle
                    </Button>
                  </div>

                  <Button
                    variant="primary"
                    size="sm"
                    onClick={handleAcceptProposal}
                    disabled={isAccepting}
                    leftIcon={<Check className="h-4 w-4" />}
                    className="bg-indigo-600 hover:bg-indigo-700 text-white font-semibold text-xs min-h-[40px] px-4 rounded-xl shadow-md"
                  >
                    {isAccepting ? 'Uygulanıyor...' : 'Planı Uygula'}
                  </Button>
                </div>
              </div>

              {/* Summary Strip */}
              <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                <Card className="p-3.5 bg-white border border-neutral-100">
                  <span className="text-xs text-neutral-500 block">Planlanan Çalışma</span>
                  <p className="text-lg font-bold text-neutral-900 mt-1">
                    {studyPlan.totalPlannedMinutes} dk
                    <span className="text-xs font-normal text-neutral-400 ml-1">
                      / {studyPlan.totalAvailableMinutes} dk
                    </span>
                  </p>
                </Card>

                <Card className="p-3.5 bg-white border border-neutral-100">
                  <span className="text-xs text-neutral-500 block">Dağıtılan Oturum</span>
                  <p className="text-lg font-bold text-neutral-900 mt-1">
                    {studyPlan.days.reduce((acc, d) => acc + d.sessionCount, 0)} Oturum
                  </p>
                </Card>

                <Card className="p-3.5 bg-white border border-neutral-100">
                  <span className="text-xs text-neutral-500 block">Esnek Süre</span>
                  <p className="text-lg font-bold text-neutral-900 mt-1">
                    {studyPlan.unallocatedMinutes} dk
                  </p>
                </Card>

                <Card className="p-3.5 bg-white border border-neutral-100">
                  <span className="text-xs text-neutral-500 block">Kapasite Kullanımı</span>
                  <p className="text-lg font-bold text-indigo-600 mt-1">
                    %{Math.round(studyPlan.utilizationRate * 100)}
                  </p>
                </Card>
              </div>

              {/* Day Selector */}
              <div className="flex items-center gap-1.5 overflow-x-auto pb-1 no-scrollbar">
                {studyPlan.days.map((day) => {
                  const isSelected = day.dayIndex === selectedProposalDayIndex;
                  return (
                    <button
                      key={day.dayIndex}
                      type="button"
                      onClick={() => setSelectedProposalDayIndex(day.dayIndex)}
                      className={`flex-1 min-w-[100px] p-2.5 rounded-xl border text-center transition-all ${
                        isSelected
                          ? 'border-indigo-500 bg-indigo-600 text-white font-semibold shadow-sm'
                          : 'border-neutral-100 bg-white text-neutral-700 hover:bg-neutral-50'
                      }`}
                    >
                      <p className="text-xs uppercase tracking-wider font-mono">
                        Gün {day.dayIndex}
                      </p>
                      <p className={`text-[11px] mt-0.5 ${isSelected ? 'text-indigo-100' : 'text-neutral-400'}`}>
                        {day.plannedMinutes} dk ({day.sessionCount} oturum)
                      </p>
                    </button>
                  );
                })}
              </div>

              {/* Sessions List */}
              {selectedProposalDay && (
                <Card className="p-4 sm:p-5 bg-white border border-neutral-100 space-y-4">
                  <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
                    <div className="flex items-center gap-2">
                      <Calendar className="h-5 w-5 text-indigo-600" />
                      <h4 className="font-bold text-neutral-900 text-base">
                        Gün {selectedProposalDay.dayIndex} Oturum Programı
                      </h4>
                    </div>
                    <Badge variant="secondary">{selectedProposalDay.plannedMinutes} dk</Badge>
                  </div>

                  {selectedProposalDay.sessions.length > 0 ? (
                    <div className="space-y-3">
                      {selectedProposalDay.sessions.map((session) => (
                        <div
                          key={`${session.dayIndex}-${session.sequence}`}
                          className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 bg-neutral-50/70 rounded-xl border border-neutral-100"
                        >
                          <div className="flex items-start gap-3">
                            <div className="w-7 h-7 rounded-lg bg-indigo-100 text-indigo-800 text-xs font-bold flex items-center justify-center shrink-0 mt-0.5">
                              {session.sequence}
                            </div>
                            <div>
                              <div className="flex items-center gap-2">
                                <span className="font-bold text-neutral-900 text-sm">
                                  {session.name}
                                </span>
                                <span className="text-xs text-neutral-400 font-mono">
                                  ({session.code})
                                </span>
                              </div>
                              <p className="text-xs text-neutral-500 mt-0.5">
                                {session.nodeType === 'QuestionStyle'
                                  ? 'Soru Tarzı Odağı'
                                  : [session.subjectName, session.topicName, session.subTopicName].filter(Boolean).join(' → ')}
                              </p>
                            </div>
                          </div>

                          <div className="flex items-center gap-2 shrink-0 self-end sm:self-auto">
                            <Badge variant={getClassificationBadgeVariant(session.classificationState)}>
                              {getClassificationLabel(session.classificationState)}
                            </Badge>
                            <span className="inline-flex items-center gap-1 text-xs text-neutral-600 bg-white border border-neutral-200 px-2.5 py-1 rounded-full font-medium">
                              <Clock className="h-3.5 w-3.5 text-neutral-400" />
                              {session.durationMinutes} dk
                            </span>
                          </div>
                        </div>
                      ))}
                    </div>
                  ) : (
                    <div className="py-8 text-center text-xs text-neutral-500">
                      Bu gün için planlanmış oturum bulunmuyor.
                    </div>
                  )}
                </Card>
              )}
            </>
          )}
        </div>
      )}

      {/* Mode 2: ACTIVE PLAN */}
      {mode === 'active' && (
        <div className="space-y-4 sm:space-y-6">
          {isLoadingActivePlan ? (
            <Card className="p-8 text-center bg-white border border-neutral-100">
              <RefreshCw className="h-8 w-8 text-neutral-400 animate-spin mx-auto mb-2" />
              <p className="text-xs text-neutral-500">Aktif plan yükleniyor...</p>
            </Card>
          ) : !activePlanDetail ? (
            <Card className="p-8 text-center bg-white border border-neutral-100">
              <Calendar className="h-10 w-10 text-neutral-300 mx-auto mb-3" />
              <h3 className="text-sm font-semibold text-neutral-900">Aktif Çalışma Planınız Bulunmuyor</h3>
              <p className="text-xs text-neutral-500 mt-1 max-w-md mx-auto">
                Önerilen plan sekmesinden "Planı Uygula" butonuna tıklayarak yeni bir çalışma planı başlatabilirsiniz.
              </p>
              <Button
                variant="primary"
                size="sm"
                onClick={() => setMode('proposal')}
                className="mt-4 text-xs py-1.5 px-4"
              >
                Önerilen Plana Git
              </Button>
            </Card>
          ) : (
            <>
              {/* Active Plan Header & Progress Card */}
              <Card className="p-5 bg-white border border-neutral-200/80 shadow-soft-sm space-y-4">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-100 pb-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <h2 className="font-bold text-neutral-900 text-lg">
                        {activePlanDetail.plan.title}
                      </h2>
                      <Badge
                        variant={
                          activePlanDetail.plan.status === 'Active'
                            ? 'success'
                            : activePlanDetail.plan.status === 'Completed'
                            ? 'info'
                            : 'secondary'
                        }
                      >
                        {activePlanDetail.plan.status === 'Active'
                          ? 'Aktif'
                          : activePlanDetail.plan.status === 'Completed'
                          ? 'Tamamlandı'
                          : 'İptal Edildi'}
                      </Badge>
                    </div>
                    <p className="text-xs text-neutral-500 mt-1">
                      Başlangıç: {new Date(activePlanDetail.plan.startDate).toLocaleDateString('tr-TR')} — Bitiş: {new Date(activePlanDetail.plan.endDate).toLocaleDateString('tr-TR')}
                    </p>
                  </div>

                  {activePlanDetail.plan.status === 'Active' && (
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => setShowCancelConfirm(true)}
                      leftIcon={<XCircle className="h-4 w-4 text-red-500" />}
                      className="text-xs text-red-600 hover:bg-red-50 hover:border-red-200 self-end sm:self-auto"
                    >
                      Planı İptal Et
                    </Button>
                  )}
                </div>

                {/* Progress Indicators */}
                <div className="space-y-3">
                  <div>
                    <div className="flex items-center justify-between text-xs font-semibold text-neutral-700 mb-1">
                      <span>Görev İlerlemesi</span>
                      <span>
                        {activePlanDetail.plan.completedTaskCount} / {activePlanDetail.plan.totalTaskCount} Tamamlandı (%{Math.round((activePlanDetail.plan.taskProgressRate || 0) * 100)})
                      </span>
                    </div>
                    <Progress value={Math.round((activePlanDetail.plan.taskProgressRate || 0) * 100)} className="h-2.5 bg-neutral-100" />
                  </div>

                  <div>
                    <div className="flex items-center justify-between text-xs font-semibold text-neutral-700 mb-1">
                      <span>Süre İlerlemesi</span>
                      <span>
                        {activePlanDetail.plan.completedTaskMinutes} dk / {activePlanDetail.plan.totalPlannedMinutes} dk (%{Math.round((activePlanDetail.plan.minuteProgressRate || 0) * 100)})
                      </span>
                    </div>
                    <Progress value={Math.round((activePlanDetail.plan.minuteProgressRate || 0) * 100)} className="h-2 bg-neutral-100" />
                  </div>
                </div>
              </Card>

              {/* Cancel Confirmation Dialog */}
              {showCancelConfirm && (
                <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Plan İptal Onayı">
                  <div className="space-y-2">
                    <p className="text-xs text-red-800">
                      Aktif çalışma planınızı iptal etmek istediğinizden emin misiniz? İptal edilen planlar salt okunur duruma gelir.
                    </p>
                    <div className="flex items-center gap-2 pt-1">
                      <Button
                        variant="primary"
                        size="sm"
                        onClick={handleCancelPlan}
                        disabled={isCancelling}
                        className="bg-red-600 hover:bg-red-700 text-white text-xs py-1"
                      >
                        {isCancelling ? 'İptal Ediliyor...' : 'Evet, İptal Et'}
                      </Button>
                      <Button
                        variant="secondary"
                        size="sm"
                        onClick={() => setShowCancelConfirm(false)}
                        className="text-xs py-1"
                      >
                        Vazgeç
                      </Button>
                    </div>
                  </div>
                </Alert>
              )}

              {/* Days & Task Completion List */}
              <div className="space-y-4">
                {/* Day selector for active plan */}
                <div className="flex items-center gap-1.5 overflow-x-auto pb-1 no-scrollbar">
                  {Array.from({ length: activePlanDetail.plan.planDays }, (_, i) => i + 1).map((dayIndex) => {
                    const dayTasks = activePlanDetail.tasks.filter((t) => t.dayIndex === dayIndex);
                    const isSelected = dayIndex === selectedActiveDayIndex;
                    const completedInDay = dayTasks.filter((t) => t.isCompleted).length;

                    return (
                      <button
                        key={dayIndex}
                        type="button"
                        onClick={() => setSelectedActiveDayIndex(dayIndex)}
                        className={`flex-1 min-w-[100px] p-2.5 rounded-xl border text-center transition-all ${
                          isSelected
                            ? 'border-primary-500 bg-primary-600 text-white font-semibold shadow-sm'
                            : 'border-neutral-100 bg-white text-neutral-700 hover:bg-neutral-50'
                        }`}
                      >
                        <p className="text-xs uppercase tracking-wider font-mono">
                          Gün {dayIndex}
                        </p>
                        <p className={`text-[11px] mt-0.5 ${isSelected ? 'text-primary-100' : 'text-neutral-400'}`}>
                          {completedInDay}/{dayTasks.length} Görev
                        </p>
                      </button>
                    );
                  })}
                </div>

                {/* Task items for selected day */}
                <Card className="p-4 sm:p-5 bg-white border border-neutral-100 space-y-3">
                  <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
                    <h4 className="font-bold text-neutral-900 text-base">
                      Gün {selectedActiveDayIndex} Görevleri
                    </h4>
                    {activePlanDetail.plan.status !== 'Active' && (
                      <Badge variant="secondary" className="text-[11px]">
                        Plan Salt Okunur ({activePlanDetail.plan.status})
                      </Badge>
                    )}
                  </div>

                  {activePlanDetail.tasks.filter((t) => t.dayIndex === selectedActiveDayIndex).length > 0 ? (
                    <div className="space-y-2.5">
                      {activePlanDetail.tasks
                        .filter((t) => t.dayIndex === selectedActiveDayIndex)
                        .map((task) => {
                          const isTerminal = activePlanDetail.plan.status !== 'Active';
                          const isPendingThis = pendingTaskId === task.id;

                          return (
                            <div
                              key={task.id}
                              className={`flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-3.5 rounded-xl border transition-all ${
                                task.isCompleted
                                  ? 'bg-emerald-50/60 border-emerald-100'
                                  : 'bg-neutral-50/70 border-neutral-100'
                              }`}
                            >
                              <div className="flex items-start gap-3">
                                <button
                                  type="button"
                                  onClick={() => handleToggleTaskCompletion(task.id, task.isCompleted)}
                                  disabled={isTerminal || isPendingThis}
                                  className={`mt-0.5 rounded-lg p-0.5 transition-colors ${
                                    isTerminal
                                      ? 'cursor-not-allowed opacity-60'
                                      : 'hover:bg-white'
                                  }`}
                                >
                                  {isPendingThis ? (
                                    <RefreshCw className="h-5 w-5 text-neutral-400 animate-spin" />
                                  ) : task.isCompleted ? (
                                    <CheckCircle2 className="h-5 w-5 text-emerald-600" />
                                  ) : (
                                    <Circle className="h-5 w-5 text-neutral-300 hover:text-primary-500" />
                                  )}
                                </button>

                                <div>
                                  <div className="flex items-center gap-2">
                                    <span className={`font-bold text-sm ${task.isCompleted ? 'text-emerald-900 line-through' : 'text-neutral-900'}`}>
                                      {task.titleSnapshot}
                                    </span>
                                  </div>
                                  <p className="text-xs text-neutral-500 mt-0.5">
                                    {task.nodeType === 'QuestionStyle'
                                      ? 'Soru Tarzı Odağı'
                                      : [task.subjectName, task.topicName, task.learningOutcomeName].filter(Boolean).join(' → ')}
                                  </p>
                                </div>
                              </div>

                              <div className="flex items-center gap-2 shrink-0 self-end sm:self-auto">
                                <Badge variant={getClassificationBadgeVariant(task.classificationStateSnapshot)}>
                                  {getClassificationLabel(task.classificationStateSnapshot)}
                                </Badge>
                                <span className="inline-flex items-center gap-1 text-xs text-neutral-600 bg-white border border-neutral-200 px-2.5 py-1 rounded-full font-medium">
                                  <Clock className="h-3.5 w-3.5 text-neutral-400" />
                                  {task.durationMinutes} dk
                                </span>
                              </div>
                            </div>
                          );
                        })}
                    </div>
                  ) : (
                    <div className="py-8 text-center text-xs text-neutral-500">
                      Bu gün için görev bulunmuyor.
                    </div>
                  )}
                </Card>
              </div>
            </>
          )}
        </div>
      )}

      {/* Mode 3: PLAN HISTORY */}
      {mode === 'history' && (
        <div className="space-y-4 sm:space-y-6">
          {isLoadingHistory ? (
            <Card className="p-8 text-center bg-white border border-neutral-100">
              <RefreshCw className="h-8 w-8 text-neutral-400 animate-spin mx-auto mb-2" />
              <p className="text-xs text-neutral-500">Plan geçmişi yükleniyor...</p>
            </Card>
          ) : historyPlans.length === 0 ? (
            <Card className="p-8 text-center bg-white border border-neutral-100">
              <History className="h-10 w-10 text-neutral-300 mx-auto mb-3" />
              <h3 className="text-sm font-semibold text-neutral-900">Geçmiş Plan Bulunmuyor</h3>
              <p className="text-xs text-neutral-500 mt-1">
                Tamamlanmış veya iptal edilmiş çalışma planlarınız burada listelenecektir.
              </p>
            </Card>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {historyPlans.map((hp) => (
                <Card
                  key={hp.id}
                  className="p-4 bg-white border border-neutral-100 hover:border-neutral-200 transition-all cursor-pointer"
                  onClick={() => handleSelectHistoryPlan(hp.id)}
                >
                  <div className="flex items-center justify-between border-b border-neutral-100 pb-2.5">
                    <h4 className="font-bold text-neutral-900 text-sm">{hp.title}</h4>
                    <Badge
                      variant={
                        hp.status === 'Active'
                          ? 'success'
                          : hp.status === 'Completed'
                          ? 'info'
                          : 'secondary'
                      }
                    >
                      {hp.status === 'Active'
                        ? 'Aktif'
                        : hp.status === 'Completed'
                        ? 'Tamamlandı'
                        : 'İptal Edildi'}
                    </Badge>
                  </div>

                  <div className="mt-3 space-y-2 text-xs text-neutral-600">
                    <div className="flex justify-between">
                      <span>Tarih:</span>
                      <span className="font-medium text-neutral-900">
                        {new Date(hp.startDate).toLocaleDateString('tr-TR')} — {new Date(hp.endDate).toLocaleDateString('tr-TR')}
                      </span>
                    </div>

                    <div className="flex justify-between">
                      <span>Görev İlerlemesi:</span>
                      <span className="font-semibold text-neutral-900">
                        {hp.completedTaskCount} / {hp.totalTaskCount} (%{Math.round((hp.taskProgressRate || 0) * 100)})
                      </span>
                    </div>

                    <Progress value={Math.round((hp.taskProgressRate || 0) * 100)} className="h-1.5 bg-neutral-100" />
                  </div>
                </Card>
              ))}
            </div>
          )}

          {/* Historical Plan Task Detail Dialog */}
          {selectedHistoryPlanDetail && (
            <Card className="p-5 bg-white border border-neutral-200 shadow-soft-md space-y-4">
              <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
                <div>
                  <h3 className="font-bold text-neutral-900 text-base">
                    {selectedHistoryPlanDetail.plan.title} (Salt Okunur)
                  </h3>
                  <p className="text-xs text-neutral-500 mt-0.5">
                    Statü: {selectedHistoryPlanDetail.plan.status} — Toplam Görev: {selectedHistoryPlanDetail.plan.totalTaskCount}
                  </p>
                </div>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setSelectedHistoryPlanDetail(null)}
                  className="text-xs py-1"
                >
                  Kapat
                </Button>
              </div>

              <div className="space-y-2 max-h-96 overflow-y-auto pr-1">
                {selectedHistoryPlanDetail.tasks.map((task) => (
                  <div
                    key={task.id}
                    className="flex items-center justify-between p-3 bg-neutral-50 rounded-xl text-xs"
                  >
                    <div className="flex items-center gap-2">
                      {task.isCompleted ? (
                        <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0" />
                      ) : (
                        <Circle className="h-4 w-4 text-neutral-300 shrink-0" />
                      )}
                      <span className={task.isCompleted ? 'line-through text-neutral-500' : 'font-medium text-neutral-900'}>
                        {task.titleSnapshot}
                      </span>
                    </div>
                    <Badge variant={getClassificationBadgeVariant(task.classificationStateSnapshot)}>
                      {getClassificationLabel(task.classificationStateSnapshot)}
                    </Badge>
                  </div>
                ))}
              </div>
            </Card>
          )}
        </div>
      )}
    </div>
  );
};
