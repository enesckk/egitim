import React from 'react';
import { cn } from '@/lib/utils';
import { DayScheduleSummary } from '../types';

export interface DaySelectorRowProps {
  days: DayScheduleSummary[];
  selectedDayIndex: number;
  onSelectDay: (index: number) => void;
}

export const DaySelectorRow: React.FC<DaySelectorRowProps> = ({
  days,
  selectedDayIndex,
  onSelectDay,
}) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-2.5 sm:p-3 flex gap-1.5 sm:gap-2 overflow-x-auto select-none">
      {days.map((day) => {
        const isSelected = day.dayIndex === selectedDayIndex;
        const isAllDone = day.taskCount > 0 && day.completedCount === day.taskCount;

        return (
          <button
            key={day.dayIndex}
            type="button"
            onClick={() => onSelectDay(day.dayIndex)}
            className={cn(
              'flex-1 min-w-[44px] py-2 px-1 rounded-xl flex flex-col items-center justify-center transition-all min-h-[52px]',
              isSelected
                ? 'bg-primary-600 text-white shadow-soft-sm font-semibold'
                : day.isToday
                ? 'bg-primary-50 text-primary-700 border border-primary-200 font-medium'
                : 'bg-neutral-50 text-neutral-600 hover:bg-neutral-100'
            )}
          >
            <span className="text-[10px] uppercase font-medium opacity-80 leading-none mb-1">
              {day.dayName}
            </span>
            <span className="font-mono text-xs sm:text-sm leading-none font-bold">
              {day.dateStr.split(' ')[0]}
            </span>
            <span
              className={cn(
                'text-[9px] mt-1 font-mono leading-none',
                isSelected
                  ? 'text-primary-100'
                  : isAllDone
                  ? 'text-success font-semibold'
                  : 'text-neutral-400'
              )}
            >
              {day.completedCount}/{day.taskCount}
            </span>
          </button>
        );
      })}
    </div>
  );
};
