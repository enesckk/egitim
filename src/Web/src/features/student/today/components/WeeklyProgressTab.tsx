import React from 'react';
import { cn } from '@/lib/utils';
import { WeeklyStreak } from '@/components/ui/WeeklyStreak';
import { WeeklyProgressData } from '../types';

export interface WeeklyProgressTabProps {
  progressData: WeeklyProgressData;
}

export const WeeklyProgressTab: React.FC<WeeklyProgressTabProps> = ({ progressData }) => {
  const streakDays = progressData.days.map((d) => ({
    dayLabel: d.label,
    isCompleted: d.done,
    isToday: d.today,
  }));

  const completedDaysCount = progressData.days.filter((d) => d.done).length;

  return (
    <div className="p-5 space-y-5">
      {/* 1. Weekly Streak */}
      <div>
        <div className="flex items-center justify-between mb-3">
          <span className="text-sm font-medium text-neutral-700">Haftalık Seri</span>
          <span className="text-xs font-mono text-neutral-500">
            {completedDaysCount}/{progressData.days.length} gün
          </span>
        </div>
        <WeeklyStreak days={streakDays} />
      </div>

      {/* 2. Weekly Hours Progress */}
      <div>
        <div className="flex items-center justify-between mb-2">
          <span className="text-sm font-medium text-neutral-700">Haftalık Çalışma</span>
          <span className="text-xs font-mono text-neutral-500">
            {progressData.completedHours} sa / {progressData.targetHours} sa hedef
          </span>
        </div>
        <div className="h-2 bg-neutral-100 rounded-full overflow-hidden">
          <div
            className="h-full bg-warning rounded-full transition-all duration-700"
            style={{ width: `${progressData.adherencePercentage}%` }}
          />
        </div>
        <p className="text-xs text-neutral-400 mt-1.5">
          {progressData.remainingHours} saat kaldı — harika gidiyorsun!
        </p>
      </div>

      {/* 3. Subject Breakdown */}
      <div>
        <p className="text-sm font-medium text-neutral-700 mb-3">Bu Hafta — Konulara Göre</p>
        <div className="space-y-2">
          {progressData.subjects.map((item) => (
            <div key={item.subject} className="flex items-center gap-3">
              <span className="text-xs text-neutral-600 w-20 truncate">{item.subject}</span>
              <div className="flex-1 h-1.5 bg-neutral-100 rounded-full overflow-hidden">
                <div
                  className={cn('h-full rounded-full', item.colorClass)}
                  style={{ width: `${(item.hours / 7) * 100}%` }}
                />
              </div>
              <span className="text-xs font-mono text-neutral-400 w-10 text-right">
                {item.hours}sa
              </span>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
