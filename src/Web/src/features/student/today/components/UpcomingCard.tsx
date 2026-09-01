import React from 'react';
import { Calendar } from 'lucide-react';
import { cn } from '@/lib/utils';
import { UpcomingEventItem } from '../types';

export interface UpcomingCardProps {
  events: UpcomingEventItem[];
}

export const UpcomingCard: React.FC<UpcomingCardProps> = ({ events }) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-4">
      <div className="flex items-center gap-2 mb-3">
        <Calendar className="h-4 w-4 text-neutral-400" />
        <h3 className="text-sm font-semibold text-neutral-700">Yaklaşan</h3>
      </div>
      <div className="space-y-3">
        {events.map((item) => (
          <div key={item.id} className="flex items-start gap-3">
            <div
              className={cn(
                'w-1.5 h-1.5 rounded-full mt-1.5 flex-shrink-0',
                item.type === 'exam' ? 'bg-danger' : 'bg-primary-400'
              )}
            />
            <div className="min-w-0">
              <p className="text-xs font-medium text-neutral-700 leading-tight">
                {item.label}
              </p>
              <p className="text-xs text-neutral-400 mt-0.5">{item.date}</p>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
