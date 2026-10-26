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
    <div className="bg-white rounded-2xl border border-neutral-200/70 p-2 sm:p-2.5 flex gap-1.5 sm:gap-2 overflow-x-auto no-scrollbar shadow-xs select-none">
      {days.map((day) => {
        const isSelected = day.dayIndex === selectedDayIndex;
        const isAllDone = day.taskCount > 0 && day.completedCount === day.taskCount;
        const hasTasks = day.taskCount > 0;

        return (
          <button
            key={day.dayIndex}
            data-testid={`day-selector-${day.dayIndex}`}
            type="button"
            onClick={() => onSelectDay(day.dayIndex)}
            className={cn(
              'flex-1 min-w-[46px] py-2 sm:py-2.5 px-1 rounded-xl flex flex-col items-center justify-center transition-all min-h-[58px] cursor-pointer focus:outline-none',
              isSelected
                ? 'bg-[#14233E] text-white shadow-sm font-semibold'
                : day.isToday
                ? 'bg-blue-50 text-[#315FE8] border border-blue-200/80 font-medium'
                : 'bg-neutral-50/80 text-neutral-600 hover:bg-neutral-100/80'
            )}
          >
            <span
              className={cn(
                'text-[10px] uppercase font-semibold leading-none mb-1 tracking-wider',
                isSelected ? 'text-blue-200' : 'text-neutral-400'
              )}
            >
              {day.dayName}
            </span>
            <span
              data-testid="day-date-number"
              className={cn(
                'text-xs sm:text-sm leading-none font-bold',
                isSelected ? 'text-white' : 'text-[#14233E]'
              )}
            >
              {day.dateStr.split(' ')[0]}
            </span>
            <span
              className={cn(
                'text-[10px] mt-1.5 font-medium leading-none',
                isSelected
                  ? 'text-blue-100'
                  : isAllDone
                  ? 'text-emerald-600 font-semibold'
                  : hasTasks
                  ? 'text-neutral-500'
                  : 'text-neutral-300'
              )}
            >
              {hasTasks ? `${day.completedCount}/${day.taskCount}` : '—'}
            </span>
          </button>
        );
      })}
    </div>
  );
};
