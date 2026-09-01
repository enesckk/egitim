import React from 'react';
import { Check } from 'lucide-react';
import { cn } from '@/lib/utils';

export interface DayStreak {
  dayLabel: string; // 'Pzt', 'Sal', 'Çar', etc.
  isCompleted: boolean;
  isToday?: boolean;
  hours?: number;
}

export interface WeeklyStreakProps {
  days: DayStreak[];
  className?: string;
}

export const WeeklyStreak: React.FC<WeeklyStreakProps> = ({ days, className }) => {
  return (
    <div className={cn('flex gap-2 w-full', className)}>
      {days.map((day) => (
        <div key={day.dayLabel} className="flex-1 flex flex-col items-center gap-1.5">
          <div
            className={cn(
              'w-full aspect-square rounded-lg flex items-center justify-center transition-all',
              day.isToday
                ? 'bg-primary-50 border-2 border-primary-400 text-primary-600 font-semibold'
                : day.isCompleted
                ? 'bg-success-light text-success'
                : 'bg-neutral-100 text-neutral-300'
            )}
          >
            {day.isCompleted && !day.isToday ? (
              <Check className="h-3.5 w-3.5 text-success stroke-[2.5]" />
            ) : day.isToday ? (
              <span className="w-2 h-2 rounded-full bg-primary-500" />
            ) : null}
          </div>
          <span className="text-[10px] text-neutral-400 font-medium">{day.dayLabel}</span>
        </div>
      ))}
    </div>
  );
};
