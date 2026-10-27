import React from 'react';
import { ChevronRight, Calendar, MessageSquare, AlertTriangle, CheckCircle2, Clock } from 'lucide-react';
import { AssignedStudentDetail } from '../types';

export interface StudentDirectoryItemProps {
  student: AssignedStudentDetail;
  onClick: (student: AssignedStudentDetail) => void;
  onNavigateTab?: (studentId: string, tab: 'plan' | 'tasks' | 'notes' | 'overview') => void;
  onSendMessage?: (studentId: string) => void;
}

export const StudentDirectoryItem: React.FC<StudentDirectoryItemProps> = ({
  student,
  onClick,
  onNavigateTab,
  onSendMessage,
}) => {
  return (
    <div
      onClick={() => onClick(student)}
      className="bg-white rounded-2xl border border-neutral-200/80 hover:border-primary-300 hover:shadow-soft-sm transition-all p-4 sm:p-5 cursor-pointer select-none"
    >
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4">
        {/* Left: Avatar & Identity Card */}
        <div className="flex items-center gap-3.5 min-w-0">
          <div className="w-11 h-11 rounded-2xl bg-[#14233E] text-white font-bold text-sm flex items-center justify-center shrink-0 shadow-soft-xs">
            {student.initials}
          </div>

          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-sm sm:text-base font-bold text-neutral-900 truncate">
                {student.name}
              </h3>

              <span className="text-[11px] font-semibold px-2.5 py-0.5 rounded-full bg-navy-50 text-navy-800 border border-navy-100">
                {student.grade}
              </span>

              {student.hasActivePlan ? (
                student.status === 'kritik' ? (
                  <span className="inline-flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full bg-rose-50 text-rose-700 border border-rose-200">
                    <AlertTriangle className="h-3 w-3" /> Geride
                  </span>
                ) : student.status === 'dikkat' ? (
                  <span className="inline-flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200">
                    <Clock className="h-3 w-3" /> Takipte
                  </span>
                ) : (
                  <span className="inline-flex items-center gap-1 text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
                    <CheckCircle2 className="h-3 w-3" /> Plan Uyumlu
                  </span>
                )
              ) : (
                <span className="inline-flex items-center gap-1 text-[10px] font-medium px-2 py-0.5 rounded-full bg-neutral-100 text-neutral-600 border border-neutral-200">
                  Aktif Plan Yok
                </span>
              )}
            </div>

            <p className="text-xs text-neutral-500 mt-1 truncate">
              {student.schoolName || student.field || 'Kayıtlı Öğrenci'} • {student.exam}
            </p>
          </div>
        </div>

        {/* Center / Right: Real Plan Metrics & Quick Actions */}
        <div className="flex items-center justify-between lg:justify-end gap-4 sm:gap-6 border-t lg:border-t-0 pt-3 lg:pt-0 border-neutral-100">
          {student.hasActivePlan && typeof student.planAdherence === 'number' ? (
            <div className="flex items-center gap-3">
              <div className="text-left sm:text-right">
                <span className="text-[10px] uppercase font-bold tracking-wider text-neutral-400 block">
                  Plan İlerlemesi
                </span>
                <span className="text-sm font-bold text-neutral-900 font-mono">
                  %{student.planAdherence}
                </span>
              </div>
              <div className="w-16 sm:w-20 bg-neutral-100 h-2 rounded-full overflow-hidden shrink-0">
                <div
                  className={`h-full rounded-full transition-all ${
                    student.planAdherence < 40
                      ? 'bg-rose-500'
                      : student.planAdherence < 70
                      ? 'bg-amber-500'
                      : 'bg-emerald-500'
                  }`}
                  style={{ width: `${Math.min(student.planAdherence, 100)}%` }}
                />
              </div>
            </div>
          ) : (
            <div className="text-left sm:text-right">
              <span className="text-[10px] uppercase font-bold tracking-wider text-neutral-400 block">
                Çalışma Planı
              </span>
              <span className="text-xs font-semibold text-neutral-500">
                Plan Hazırlanmadı
              </span>
            </div>
          )}

          {/* Action buttons */}
          <div className="flex items-center gap-1.5 shrink-0" onClick={(e) => e.stopPropagation()}>
            {onSendMessage && (
              <button
                type="button"
                onClick={() => onSendMessage(student.id)}
                title="Mesaj Gönder"
                className="p-2 text-neutral-500 hover:text-primary-600 hover:bg-primary-50 rounded-xl transition-colors cursor-pointer border border-neutral-200/80"
              >
                <MessageSquare className="h-4 w-4" />
              </button>
            )}

            {onNavigateTab && (
              <button
                type="button"
                onClick={() => onNavigateTab(student.id, 'plan')}
                title="Çalışma Planı"
                className="p-2 text-neutral-500 hover:text-primary-600 hover:bg-primary-50 rounded-xl transition-colors cursor-pointer border border-neutral-200/80"
              >
                <Calendar className="h-4 w-4" />
              </button>
            )}

            <button
              type="button"
              onClick={() => onClick(student)}
              className="p-2 text-primary-600 hover:text-primary-800 hover:bg-primary-50 rounded-xl transition-colors cursor-pointer border border-primary-200/80 ml-1"
              title="Öğrenci Detayı"
            >
              <ChevronRight className="h-4 w-4" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};

export default StudentDirectoryItem;
