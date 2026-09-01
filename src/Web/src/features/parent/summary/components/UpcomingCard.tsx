import React from 'react';
import { Calendar } from 'lucide-react';
import { UpcomingEventItem } from '../types';

export interface UpcomingCardProps {
  events: UpcomingEventItem[];
}

export const UpcomingCard: React.FC<UpcomingCardProps> = ({ events }) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm select-none">
      <div className="flex items-center gap-2 mb-4">
        <Calendar className="h-4 w-4 text-neutral-400" />
        <h2 className="font-semibold text-neutral-800 text-sm">Yaklaşan</h2>
      </div>
      <div className="space-y-3">
        {events.map((item) => (
          <div key={item.id} className="flex items-start gap-3">
            <div className="w-1.5 h-1.5 rounded-full bg-primary-500 mt-1.5 flex-shrink-0" />
            <div className="min-w-0">
              <p className="text-sm font-medium text-neutral-800 truncate">{item.label}</p>
              <p className="text-xs text-neutral-400 mt-0.5">
                {item.date} · {item.note}
              </p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
