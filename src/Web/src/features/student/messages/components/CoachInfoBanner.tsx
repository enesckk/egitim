import React from 'react';
import { Calendar, CheckCircle2 } from 'lucide-react';
import { Avatar } from '@/components/ui/Avatar';
import { ConversationParticipant } from '../types';

export interface CoachInfoBannerProps {
  coach: ConversationParticipant;
}

export const CoachInfoBanner: React.FC<CoachInfoBannerProps> = ({ coach }) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-3.5 sm:p-4 select-none">
      <div className="flex items-center justify-between gap-3">
        <div className="flex items-center gap-3 min-w-0">
          <div className="relative">
            <Avatar name={coach.name} size="md" variant="primary" />
            {coach.isOnline && (
              <span className="absolute bottom-0 right-0 w-2.5 h-2.5 bg-success rounded-full ring-2 ring-white" />
            )}
          </div>

          <div className="min-w-0">
            <div className="flex items-center gap-1.5">
              <h3 className="text-sm font-semibold text-neutral-900 truncate">
                {coach.name}
              </h3>
              <CheckCircle2 className="h-3.5 w-3.5 text-primary-600 flex-shrink-0" />
            </div>
            <p className="text-xs text-neutral-400 truncate">
              {coach.title} • {coach.institution}
            </p>
          </div>
        </div>

        {coach.nextMeetingDate && (
          <div className="hidden sm:flex flex-col items-end text-right bg-primary-50 border border-primary-100 rounded-xl px-3 py-1.5">
            <span className="text-[10px] uppercase font-semibold text-primary-700 flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              Sonraki Görüşme
            </span>
            <span className="font-mono text-xs font-bold text-primary-900 mt-0.5">
              {coach.nextMeetingDate}
            </span>
          </div>
        )}
      </div>
    </div>
  );
};
