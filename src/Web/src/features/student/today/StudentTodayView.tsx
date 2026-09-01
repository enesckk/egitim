import React, { useState } from 'react';
import { cn } from '@/lib/utils';
import { NextStudyCard } from './components/NextStudyCard';
import { TodayPlanTab } from './components/TodayPlanTab';
import { WeeklyProgressTab } from './components/WeeklyProgressTab';
import { RecommendationCard } from './components/RecommendationCard';
import { UpcomingCard } from './components/UpcomingCard';
import { MonthlyStatsCard } from './components/MonthlyStatsCard';
import { initialStudentTodayData } from './mockData';
import { StudentTodayViewModel, TodayPlanItem } from './types';

export interface StudentTodayViewProps {
  initialData?: StudentTodayViewModel;
  onStartStudy?: (studyTitle: string) => void;
  onRecommendationAction?: (topic: string) => void;
}

export const StudentTodayView: React.FC<StudentTodayViewProps> = ({
  initialData = initialStudentTodayData,
  onStartStudy,
  onRecommendationAction,
}) => {
  const [activeTab, setActiveTab] = useState<'plan' | 'progress'>('plan');
  const [plans, setPlans] = useState<TodayPlanItem[]>(initialData.todayPlans);

  // Local interaction: toggle status (completed <-> active <-> upcoming)
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

  const completedCount = plans.filter((t) => t.status === 'completed').length;

  return (
    <div className="max-w-5xl mx-auto select-none">
      {/* Greeting Header */}
      <div className="mb-3.5 sm:mb-6">
        <p className="text-neutral-400 text-xs sm:text-sm mb-0.5 capitalize font-medium">
          {initialData.dateString}
        </p>
        <h1 className="font-serif text-2xl sm:text-4xl text-neutral-900 tracking-tight leading-tight">
          İyi günler, {initialData.studentName}
        </h1>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3 sm:gap-4">
        {/* Left / Main Column (2 spans on desktop) */}
        <div className="lg:col-span-2 space-y-3 sm:space-y-4">
          {/* 1. Next Study Card (Dark Navy Hero) */}
          <NextStudyCard
            nextStudy={initialData.nextStudy}
            completedCount={completedCount}
            totalCount={plans.length}
            onStartStudy={() => onStartStudy?.(initialData.nextStudy.title)}
          />

          {/* 2. Today's Plan & Progress Card with Tabs */}
          <div className="bg-white rounded-2xl border border-neutral-100 overflow-hidden shadow-soft-sm">
            {/* Tabs Header */}
            <div className="flex border-b border-neutral-100">
              <button
                type="button"
                onClick={() => setActiveTab('plan')}
                className={cn(
                  'flex-1 py-2.5 sm:py-3.5 text-xs sm:text-sm font-medium transition-colors min-h-[38px] sm:min-h-[44px]',
                  activeTab === 'plan'
                    ? 'text-primary-600 border-b-2 border-primary-500 font-semibold -mb-px'
                    : 'text-neutral-500 hover:text-neutral-700'
                )}
              >
                Bugünkü Plan
              </button>

              <button
                type="button"
                onClick={() => setActiveTab('progress')}
                className={cn(
                  'flex-1 py-2.5 sm:py-3.5 text-xs sm:text-sm font-medium transition-colors min-h-[38px] sm:min-h-[44px]',
                  activeTab === 'progress'
                    ? 'text-primary-600 border-b-2 border-primary-500 font-semibold -mb-px'
                    : 'text-neutral-500 hover:text-neutral-700'
                )}
              >
                İlerleme
              </button>
            </div>

            {/* Tab Contents */}
            {activeTab === 'plan' ? (
              <TodayPlanTab
                plans={plans}
                onTogglePlanStatus={handleTogglePlanStatus}
                onContinuePlan={handleContinuePlan}
              />
            ) : (
              <WeeklyProgressTab progressData={initialData.weeklyProgress} />
            )}
          </div>
        </div>

        {/* Right Sidebar Column */}
        <div className="space-y-3 sm:space-y-4">
          {/* 3. Contextual Recommendation Card */}
          <RecommendationCard
            recommendation={initialData.recommendation}
            onAction={() =>
              onRecommendationAction?.(initialData.recommendation.topic)
            }
          />

          {/* 4. Upcoming Events Card */}
          <UpcomingCard events={initialData.upcomingEvents} />

          {/* 5. Monthly Quick Stats Card */}
          <MonthlyStatsCard stats={initialData.monthlyStats} />
        </div>
      </div>
    </div>
  );
};
