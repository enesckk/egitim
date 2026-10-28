import React from 'react';
import { Calendar } from 'lucide-react';
import { UpcomingEventItem } from '../types';

export interface UpcomingCardProps {
  events: UpcomingEventItem[];
}

export const UpcomingCard: React.FC<UpcomingCardProps> = ({ events }) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs select-none space-y-3.5">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Calendar className="h-4.5 w-4.5 text-primary-600" />
          <h2 className="font-bold text-neutral-900 text-sm sm:text-base">
            Yaklaşan Faaliyetler ve Denemeler
          </h2>
        </div>
        <span className="text-xs text-neutral-400 font-mono">
          {events.length} Kayıt
        </span>
      </div>

      {events.length === 0 ? (
        <div className="p-6 text-center text-xs text-neutral-400 bg-neutral-50/50 rounded-xl border border-neutral-100">
          Planlanmış yaklaşan deneme sınavı veya görev bulunmuyor.
        </div>
      ) : (
        <div className="divide-y divide-neutral-100">
          {events.map((item) => (
            <div key={item.id} className="py-2.5 first:pt-0 last:pb-0 flex items-center justify-between gap-3">
              <div className="flex items-start gap-2.5 min-w-0">
                <div className="w-2 h-2 rounded-full bg-[#315FE8] mt-1.5 shrink-0" />
                <div className="min-w-0">
                  <p className="text-xs sm:text-sm font-semibold text-neutral-900 truncate">
                    {item.label}
                  </p>
                  <p className="text-[11px] text-neutral-500 mt-0.5">
                    {item.date}
                  </p>
                </div>
              </div>

              <div className="shrink-0 text-right">
                <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-neutral-700 bg-[#F8F7F3] border border-neutral-200 px-2.5 py-1 rounded-lg">
                  {item.note}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
