import React from 'react';
import { Video, Building, Clock } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { CoachMeetingItem } from '../types';

export interface MeetingItemCardProps {
  meeting: CoachMeetingItem;
  onSelectStudent: (studentId: string) => void;
}

export const MeetingItemCard: React.FC<MeetingItemCardProps> = ({
  meeting,
  onSelectStudent,
}) => {
  const isUpcoming = meeting.status === 'upcoming';

  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-4 space-y-3 shadow-soft-sm select-none">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        {/* Left: Time & Student */}
        <div className="flex items-center gap-3.5 min-w-0">
          <div className="text-center w-14 flex-shrink-0 bg-surface-alt rounded-xl p-2">
            <span className="font-mono text-sm font-bold text-neutral-900 block leading-tight">
              {meeting.timeStr}
            </span>
            <span className="text-[10px] text-neutral-400 font-mono block mt-0.5">
              {meeting.durationMinutes} dk
            </span>
          </div>

          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h4
                onClick={() => onSelectStudent(meeting.studentId)}
                className="text-sm sm:text-base font-semibold text-neutral-900 hover:text-primary-600 cursor-pointer truncate transition-colors"
              >
                {meeting.studentName}
              </h4>
              <Badge
                variant={isUpcoming ? 'info' : 'success'}
                size="sm"
              >
                {isUpcoming ? 'Planlandı' : 'Tamamlandı'}
              </Badge>
            </div>

            <p className="text-xs text-neutral-500 truncate mt-0.5">
              {meeting.grade} • {meeting.field} — {meeting.type}
            </p>
          </div>
        </div>

        {/* Right: Meeting Location & Date */}
        <div className="flex items-center justify-between sm:justify-end gap-3 border-t sm:border-t-0 pt-2 sm:pt-0 border-neutral-50 text-xs text-neutral-400">
          <div className="flex items-center gap-1 font-mono">
            <Clock className="h-3.5 w-3.5 text-neutral-400" />
            <span>{meeting.dateStr}</span>
          </div>

          <div className="flex items-center gap-1 font-medium text-neutral-700 bg-neutral-50 px-2.5 py-1 rounded-lg">
            {meeting.locationType.includes('Video') ? (
              <Video className="h-3.5 w-3.5 text-primary-600" />
            ) : (
              <Building className="h-3.5 w-3.5 text-navy-600" />
            )}
            <span>{meeting.locationType}</span>
          </div>
        </div>
      </div>

      {meeting.notes && (
        <div className="text-xs text-neutral-600 bg-surface-alt rounded-xl px-3 py-2 leading-relaxed">
          <strong>Görüşme Gündemi:</strong> {meeting.notes}
        </div>
      )}
    </div>
  );
};
