import React, { useState } from 'react';
import { Video, Building, Clock, ChevronRight, CheckCircle2, XCircle, Trash2, Calendar, AlertTriangle } from 'lucide-react';
import { CoachMeetingItem } from '../types';

export interface MeetingItemCardProps {
  meeting: CoachMeetingItem;
  onSelectStudent: (studentId: string) => void;
  onStatusChange?: (meetingId: string, newStatus: 'completed' | 'cancelled' | 'upcoming') => Promise<void> | void;
  onDeleteMeeting?: (meetingId: string) => Promise<void> | void;
}

export const MeetingItemCard: React.FC<MeetingItemCardProps> = ({
  meeting,
  onSelectStudent,
  onStatusChange,
  onDeleteMeeting,
}) => {
  const [isActionLoading, setIsActionLoading] = useState(false);
  const [confirmDelete, setConfirmDelete] = useState(false);

  const isUpcoming = meeting.status === 'upcoming';
  const isCompleted = meeting.status === 'completed';
  const isCancelled = meeting.status === 'cancelled';

  // Derived "Zamanı Geçti" check: scheduled/upcoming meeting whose endTime (or startTime) has passed
  const isOverdue = React.useMemo(() => {
    if (!isUpcoming) return false;
    const timeRef = meeting.endTime || meeting.startTime;
    if (!timeRef) return false;
    const targetDate = new Date(timeRef);
    return !isNaN(targetDate.getTime()) && targetDate.getTime() < Date.now();
  }, [isUpcoming, meeting.endTime, meeting.startTime]);

  const handleComplete = async (e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onStatusChange || isActionLoading) return;
    try {
      setIsActionLoading(true);
      await onStatusChange(meeting.id, 'completed');
    } finally {
      setIsActionLoading(false);
    }
  };

  const handleCancel = async (e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onStatusChange || isActionLoading) return;
    try {
      setIsActionLoading(true);
      await onStatusChange(meeting.id, 'cancelled');
    } finally {
      setIsActionLoading(false);
    }
  };

  const handleDelete = async (e: React.MouseEvent) => {
    e.stopPropagation();
    if (!onDeleteMeeting || isActionLoading) return;
    try {
      setIsActionLoading(true);
      await onDeleteMeeting(meeting.id);
    } finally {
      setIsActionLoading(false);
      setConfirmDelete(false);
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 space-y-3 shadow-soft-xs hover:border-primary-300 transition-all select-none">
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3.5">
        {/* Left: Time & Student Identity */}
        <div className="flex items-start sm:items-center gap-3.5 min-w-0 flex-1">
          <div className="text-center w-16 shrink-0 bg-[#F8F7F3] border border-neutral-200/80 rounded-xl p-2 shadow-soft-xs">
            <span className="font-mono text-sm font-bold text-neutral-900 block leading-tight">
              {meeting.timeStr}
            </span>
            <span className="text-[10px] text-neutral-500 font-mono block mt-0.5">
              {meeting.durationMinutes} dk
            </span>
          </div>

          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h4
                onClick={() => onSelectStudent(meeting.studentId)}
                className="text-sm sm:text-base font-bold text-neutral-900 hover:text-primary-600 cursor-pointer truncate transition-colors"
              >
                {meeting.studentName}
              </h4>

              {isUpcoming && (
                <span className="inline-flex items-center gap-1 text-[10px] font-bold px-2.5 py-0.5 rounded-full bg-blue-50 text-blue-700 border border-blue-200 shrink-0">
                  <Clock className="h-3 w-3" /> Planlandı
                </span>
              )}
              {isUpcoming && isOverdue && (
                <span className="inline-flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full bg-amber-50 text-amber-800 border border-amber-300 shrink-0">
                  <AlertTriangle className="h-3 w-3 text-amber-600" /> Zamanı Geçti
                </span>
              )}
              {isCompleted && (
                <span className="inline-flex items-center gap-1 text-[10px] font-bold px-2.5 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200 shrink-0">
                  <CheckCircle2 className="h-3 w-3" /> Tamamlandı
                </span>
              )}
              {isCancelled && (
                <span className="inline-flex items-center gap-1 text-[10px] font-bold px-2.5 py-0.5 rounded-full bg-neutral-100 text-neutral-600 border border-neutral-200 shrink-0">
                  <XCircle className="h-3 w-3" /> İptal Edildi
                </span>
              )}
            </div>

            <p className="text-xs text-neutral-500 truncate mt-0.5">
              {meeting.grade} • {meeting.field || 'Sayısal'} — <span className="font-medium text-neutral-700">{meeting.type}</span>
            </p>
          </div>
        </div>

        {/* Right: Date, Location & Actions */}
        <div className="flex flex-wrap items-center justify-between lg:justify-end gap-2.5 border-t lg:border-t-0 pt-2.5 lg:pt-0 border-neutral-100 text-xs shrink-0">
          <div className="flex items-center gap-1 font-mono text-neutral-600 shrink-0">
            <Calendar className="h-3.5 w-3.5 text-neutral-400" />
            <span>{meeting.dateStr}</span>
          </div>

          <div className="flex items-center gap-1 font-medium text-navy-900 bg-navy-50/80 px-2.5 py-1 rounded-xl border border-navy-100/60 shrink-0">
            {meeting.locationType.includes('Video') ? (
              <Video className="h-3.5 w-3.5 text-primary-600" />
            ) : (
              <Building className="h-3.5 w-3.5 text-navy-800" />
            )}
            <span>{meeting.locationType}</span>
          </div>

          {/* Action buttons */}
          <div className="flex items-center gap-1.5 shrink-0 ml-auto lg:ml-1">
            {isUpcoming && onStatusChange && (
              <>
                <button
                  type="button"
                  onClick={handleComplete}
                  disabled={isActionLoading}
                  title="Tamamlandı Olarak İşaretle"
                  className="p-1.5 text-emerald-600 hover:bg-emerald-50 rounded-lg transition-colors border border-emerald-200 cursor-pointer"
                >
                  <CheckCircle2 className="h-4 w-4" />
                </button>
                <button
                  type="button"
                  onClick={handleCancel}
                  disabled={isActionLoading}
                  title="Görüşmeyi İptal Et"
                  className="p-1.5 text-amber-600 hover:bg-amber-50 rounded-lg transition-colors border border-amber-200 cursor-pointer"
                >
                  <XCircle className="h-4 w-4" />
                </button>
              </>
            )}

            {onDeleteMeeting && (
              confirmDelete ? (
                <div className="flex items-center gap-1 bg-rose-50 p-0.5 rounded-lg border border-rose-200">
                  <button
                    type="button"
                    onClick={handleDelete}
                    disabled={isActionLoading}
                    className="text-[10px] font-bold text-white bg-rose-600 hover:bg-rose-700 px-2 py-1 rounded cursor-pointer"
                  >
                    Sil
                  </button>
                  <button
                    type="button"
                    onClick={(e) => {
                      e.stopPropagation();
                      setConfirmDelete(false);
                    }}
                    className="text-[10px] text-neutral-600 hover:bg-neutral-200 px-1.5 py-1 rounded cursor-pointer"
                  >
                    Vazgeç
                  </button>
                </div>
              ) : (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    setConfirmDelete(true);
                  }}
                  title="Görüşmeyi Sil"
                  className="p-1.5 text-neutral-400 hover:text-rose-600 hover:bg-rose-50 rounded-lg transition-colors border border-neutral-200 cursor-pointer"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              )
            )}

            <button
              type="button"
              onClick={() => onSelectStudent(meeting.studentId)}
              className="p-1.5 text-primary-600 hover:bg-primary-50 rounded-lg transition-colors border border-primary-200 cursor-pointer"
              title="Öğrenci Profili"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>

      {meeting.notes && (
        <div className="text-xs text-neutral-600 bg-[#F8F7F3] rounded-xl px-3.5 py-2.5 leading-relaxed border border-neutral-200/60">
          <strong className="text-neutral-800">Görüşme Gündemi:</strong> {meeting.notes}
        </div>
      )}
    </div>
  );
};

export default MeetingItemCard;
