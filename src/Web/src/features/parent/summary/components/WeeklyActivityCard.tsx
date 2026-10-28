import React from 'react';
import { CalendarCheck, CheckCircle2 } from 'lucide-react';
import { DayActivityItem } from '../types';

export interface WeeklyActivityCardProps {
  activity: DayActivityItem[];
  planAdherencePercentage: number | null;
  hasActivePlan: boolean;
  planTitle?: string;
  completedTasksCount?: number;
  totalTasksCount?: number;
}

export const WeeklyActivityCard: React.FC<WeeklyActivityCardProps> = ({
  activity,
  planAdherencePercentage,
  hasActivePlan,
  planTitle,
  completedTasksCount = 0,
  totalTasksCount = 0,
}) => {
  const workedDays = activity.filter((d) => d.done).length;

  return (
    <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs select-none space-y-4">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <CalendarCheck className="h-4.5 w-4.5 text-[#315FE8]" />
          <h2 className="font-bold text-neutral-900 text-sm sm:text-base">
            Haftalık Çalışma ve Plan Uyumu
          </h2>
        </div>

        {hasActivePlan ? (
          <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
            Aktif Plan Devam Ediyor
          </span>
        ) : (
          <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-neutral-100 text-neutral-600 border border-neutral-200">
            Aktif Plan Yok
          </span>
        )}
      </div>

      {/* 3 Metric Counters */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        <div className="bg-[#F8F7F3] rounded-xl p-3 border border-neutral-200/60 text-center">
          <p className="font-mono text-2xl font-bold text-neutral-900 leading-tight">
            {workedDays} / 7
          </p>
          <p className="text-xs text-neutral-500 mt-0.5 font-medium">Hedefli Çalışma Günü</p>
        </div>

        <div className="bg-[#F8F7F3] rounded-xl p-3 border border-neutral-200/60 text-center">
          <p className="font-mono text-2xl font-bold text-neutral-900 leading-tight">
            {completedTasksCount} / {totalTasksCount}
          </p>
          <p className="text-xs text-neutral-500 mt-0.5 font-medium">Tamamlanan Görev</p>
        </div>

        <div className="bg-[#F8F7F3] rounded-xl p-3 border border-neutral-200/60 text-center">
          {planAdherencePercentage !== null ? (
            <>
              <p className="font-mono text-2xl font-bold text-emerald-600 leading-tight">
                %{planAdherencePercentage}
              </p>
              <p className="text-xs text-neutral-500 mt-0.5 font-medium">Plan Uyumu</p>
            </>
          ) : (
            <>
              <p className="font-semibold text-base text-neutral-400 leading-tight mt-1">
                —
              </p>
              <p className="text-xs text-neutral-400 mt-1 font-medium">Ölçülebilir Plan Yok</p>
            </>
          )}
        </div>
      </div>

      {/* 7-Day Activity Stream */}
      <div className="space-y-1.5 pt-1">
        <div className="flex gap-1.5 sm:gap-2">
          {activity.map((d) => (
            <div key={d.day} className="flex-1 flex flex-col items-center gap-1.5 min-w-0">
              <div
                className={`w-full rounded-xl flex items-center justify-center text-[10px] font-semibold h-9 transition-colors ${
                  d.today
                    ? 'border-2 border-primary-500 bg-primary-50/50 text-primary-700'
                    : d.done
                    ? 'bg-emerald-50 border border-emerald-200 text-emerald-700'
                    : 'bg-neutral-50 border border-neutral-200 text-neutral-300'
                }`}
              >
                {d.done ? <CheckCircle2 className="h-4 w-4 text-emerald-600" /> : '—'}
              </div>
              <span className={`text-[11px] font-medium ${d.today ? 'text-primary-700 font-bold' : 'text-neutral-500'}`}>
                {d.day}
              </span>
            </div>
          ))}
        </div>
        <p className="text-[11px] text-neutral-400 text-center pt-1">
          {hasActivePlan && planTitle
            ? `Takip Edilen: ${planTitle}`
            : 'Öğrencinin haftalık çalışma planı atandığında günlük ilerleme burada otomatik güncellenir.'}
        </p>
      </div>
    </div>
  );
};
