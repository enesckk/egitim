import React from 'react';
import { Calendar, ChevronRight, Video } from 'lucide-react';
import { TodaySessionItem } from '../types';

export interface TodaySessionsCardProps {
  sessions: TodaySessionItem[];
  onViewAllSessions: () => void;
  onJoinSession?: (session: TodaySessionItem) => void;
}

export const TodaySessionsCard: React.FC<TodaySessionsCardProps> = ({
  sessions,
  onViewAllSessions,
  onJoinSession,
}) => {
  return (
    <div className="space-y-3 select-none">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-primary-600" />
          <h2 className="font-semibold text-neutral-900 text-sm sm:text-base">
            Bugünkü Görüşmeler
          </h2>
        </div>
        <button
          type="button"
          onClick={onViewAllSessions}
          className="text-xs text-primary-600 hover:text-primary-700 font-medium flex items-center gap-1"
        >
          Tüm Takvim <ChevronRight className="h-3 w-3" />
        </button>
      </div>

      <div className="bg-white rounded-2xl border border-neutral-100 divide-y divide-neutral-50 shadow-soft-sm overflow-hidden">
        {sessions.map((session) => (
          <div key={session.id} className="flex items-center gap-3.5 px-4 py-3.5 hover:bg-neutral-50/50 transition-colors">
            {/* Time */}
            <div className="text-center w-12 flex-shrink-0">
              <p className="font-mono text-sm font-semibold text-neutral-800">{session.time}</p>
              <p className="text-[11px] text-neutral-400 font-mono">{session.duration}</p>
            </div>

            <div className="w-px h-8 bg-neutral-100 flex-shrink-0" />

            {/* Avatar Initials */}
            <div className="w-8 h-8 rounded-full bg-navy-50 text-navy-800 text-xs font-semibold flex items-center justify-center flex-shrink-0">
              {session.initials}
            </div>

            {/* Student details */}
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-neutral-800 truncate">{session.studentName}</p>
              <p className="text-xs text-neutral-400 truncate">{session.grade} • {session.type}</p>
            </div>

            {/* Join / Start Action */}
            <button
              type="button"
              onClick={() => onJoinSession?.(session)}
              className="text-xs text-primary-600 font-semibold hover:text-primary-700 flex items-center gap-1 bg-primary-50 hover:bg-primary-100 px-2.5 py-1.5 rounded-lg transition-colors min-h-[32px]"
            >
              <Video className="h-3 w-3" />
              Katıl
            </button>
          </div>
        ))}

        {sessions.length === 0 && (
          <div className="px-4 py-6 text-center text-xs text-neutral-400">
            Bugün için planlanmış aktif görüşme bulunmuyor.
          </div>
        )}
      </div>
    </div>
  );
};
