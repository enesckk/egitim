import React from 'react';
import { CheckCircle2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { DayActivityItem } from '../types';

export interface WeeklyActivityCardProps {
  activity: DayActivityItem[];
  planAdherencePercentage: number;
}

export const WeeklyActivityCard: React.FC<WeeklyActivityCardProps> = ({
  activity,
  planAdherencePercentage,
}) => {
  const workedDays = activity.filter((d) => d.done).length;
  const totalHours = activity.reduce((acc, d) => acc + d.hours, 0);

  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm select-none">
      <h2 className="font-semibold text-neutral-800 text-sm mb-4">Bu Hafta</h2>

      {/* 3 Metric counters */}
      <div className="flex items-center justify-between mb-5 px-2">
        <div className="text-center">
          <p className="font-mono text-2xl font-semibold text-neutral-900 leading-tight">
            {workedDays}
          </p>
          <p className="text-xs text-neutral-400 mt-0.5">çalışma günü</p>
        </div>
        <div className="text-center">
          <p className="font-mono text-2xl font-semibold text-neutral-900 leading-tight">
            {totalHours} sa
          </p>
          <p className="text-xs text-neutral-400 mt-0.5">toplam çalışma</p>
        </div>
        <div className="text-center">
          <p className="font-mono text-2xl font-semibold text-success leading-tight">
            %{planAdherencePercentage}
          </p>
          <p className="text-xs text-neutral-400 mt-0.5">plan uyumu</p>
        </div>
      </div>

      {/* 7-Day activity pattern */}
      <div className="flex gap-1.5 sm:gap-2">
        {activity.map((d) => (
          <div key={d.day} className="flex-1 flex flex-col items-center gap-1.5 min-w-0">
            <div
              className={cn(
                'w-full rounded-lg flex items-center justify-center text-[10px] font-semibold h-8 transition-colors',
                d.today
                  ? 'border-2 border-dashed border-neutral-300 text-neutral-300 bg-white'
                  : d.done
                  ? 'bg-success-light text-success'
                  : 'bg-neutral-100 text-neutral-300'
              )}
            >
              {d.done ? <CheckCircle2 className="h-4 w-4 text-success" /> : '—'}
            </div>
            <span className="text-[10px] text-neutral-400 font-medium">{d.day}</span>
          </div>
        ))}
      </div>
    </div>
  );
};
