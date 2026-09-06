import React, { useState } from 'react';
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
import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import { Skeleton } from '@/components/ui/Skeleton';
import { NextStudyCard } from './components/NextStudyCard';
import { TodayPlanTab } from './components/TodayPlanTab';
import { WeeklyProgressTab } from './components/WeeklyProgressTab';
import { RecommendationCard } from './components/RecommendationCard';
import { UpcomingCard } from './components/UpcomingCard';
import { MonthlyStatsCard } from './components/MonthlyStatsCard';
import { StudentTodayViewModel, TodayPlanItem } from './types';

export interface StudentTodayViewProps {
  initialData?: StudentTodayViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
  onStartStudy?: (studyTitle: string) => void;
  onRecommendationAction?: (topic: string) => void;
}

export const StudentTodayView: React.FC<StudentTodayViewProps> = ({
  initialData,
  isLoading = false,
  errorMessage,
  onRetry,
  onStartStudy,
  onRecommendationAction,
}) => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [activeTab, setActiveTab] = useState<'plan' | 'progress'>('plan');
  const [plans, setPlans] = useState<TodayPlanItem[]>(initialData?.todayPlans || []);

  const studentDisplayName = user?.name || user?.email?.split('@')[0] || initialData?.studentName || 'Öğrenci';
  const currentDateFormatted = new Date().toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
  });

  // Loading State
  if (isLoading) {
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
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4" data-testid="student-today-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Sayfa Yüklenemedi">
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button variant="primary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  // Local interaction: toggle status (completed <-> active <-> upcoming) when plans exist
  const handleTogglePlanStatus = (id: number) => {
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
    if (plan && onStartStudy) {
      onStartStudy(plan.topic);
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
            {initialData?.nextStudy && (
              <NextStudyCard
                nextStudy={initialData.nextStudy}
                completedCount={completedCount}
                totalCount={plans.length}
                onStartStudy={() => onStartStudy?.(initialData.nextStudy.title)}
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
                {initialData?.weeklyProgress && (
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
              ) : initialData?.weeklyProgress ? (
                <WeeklyProgressTab progressData={initialData.weeklyProgress} />
              ) : null}
            </div>
          </div>

          <div className="space-y-3.5 sm:space-y-4">
            {initialData?.recommendation && (
              <RecommendationCard
                recommendation={initialData.recommendation}
                onAction={() => onRecommendationAction?.(initialData.recommendation.topic)}
              />
            )}
            {initialData?.upcomingEvents && initialData.upcomingEvents.length > 0 && (
              <UpcomingCard events={initialData.upcomingEvents} />
            )}
            {initialData?.monthlyStats && initialData.monthlyStats.length > 0 && (
              <MonthlyStatsCard stats={initialData.monthlyStats} />
            )}
          </div>
        </div>
      ) : (
        /* Honest Calm Academic Shell (When no planning backend exists yet) */
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

          {/* Right Column: Institutional Status Summary */}
          <div className="space-y-4">
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
          </div>
        </div>
      )}
    </div>
  );
};
