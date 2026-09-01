import React from 'react';
import { AlertTriangle, XCircle, Info, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { AttentionSeverity, AttentionStudentItem } from '../types';

export interface AttentionStudentsCardProps {
  students: AttentionStudentItem[];
  onActionClick: (student: AttentionStudentItem) => void;
}

const SEVERITY_STYLES: Record<
  AttentionSeverity,
  { border: string; bg: string; icon: React.ReactNode; badge: string; text: string }
> = {
  danger: {
    border: 'border-red-200',
    bg: 'bg-red-50/40',
    icon: <XCircle className="h-4 w-4 text-danger flex-shrink-0 mt-0.5" />,
    badge: 'bg-danger-light text-danger-dark border-red-200',
    text: 'Acil Müdahale',
  },
  warning: {
    border: 'border-amber-200',
    bg: 'bg-amber-50/40',
    icon: <AlertTriangle className="h-4 w-4 text-warning flex-shrink-0 mt-0.5" />,
    badge: 'bg-warning-light text-warning-dark border-amber-200',
    text: 'Takip Gerekiyor',
  },
  info: {
    border: 'border-blue-200',
    bg: 'bg-blue-50/40',
    icon: <Info className="h-4 w-4 text-primary-600 flex-shrink-0 mt-0.5" />,
    badge: 'bg-primary-50 text-primary-700 border-primary-200',
    text: 'Bilgi',
  },
};

export const AttentionStudentsCard: React.FC<AttentionStudentsCardProps> = ({
  students,
  onActionClick,
}) => {
  return (
    <div className="space-y-3 select-none">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 text-danger" />
          <h2 className="font-semibold text-neutral-900 text-sm sm:text-base">
            Dikkat Gerektiren Öğrenciler
          </h2>
          <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-danger text-white text-xs font-bold">
            {students.length}
          </span>
        </div>
      </div>

      <div className="space-y-2.5">
        {students.map((student) => {
          const style = SEVERITY_STYLES[student.severity];

          return (
            <div
              key={student.id}
              className={cn(
                'bg-white rounded-2xl border p-4 sm:p-4.5 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-soft-sm transition-all',
                style.border,
                style.bg
              )}
            >
              <div className="space-y-1.5 min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <span className="font-semibold text-sm sm:text-base text-neutral-900">
                    {student.name}
                  </span>
                  <span className="text-xs text-neutral-500">
                    {student.grade} • {student.field}
                  </span>
                  <span
                    className={cn(
                      'text-[10px] uppercase font-semibold px-2 py-0.5 rounded-full border',
                      style.badge
                    )}
                  >
                    {style.text}
                  </span>
                </div>

                <div className="flex items-start gap-2">
                  {style.icon}
                  <p className="text-xs text-neutral-700 leading-relaxed">
                    {student.issue}
                  </p>
                </div>

                <p className="text-[11px] text-neutral-400">
                  Son etkinlik: {student.lastSeen}
                </p>
              </div>

              <button
                type="button"
                onClick={() => onActionClick(student)}
                className="flex items-center justify-center gap-1 text-xs font-semibold text-primary-700 bg-white border border-primary-200 px-3 py-2 rounded-xl hover:bg-primary-50 transition-colors min-h-[38px] whitespace-nowrap self-start sm:self-center shadow-soft-sm"
              >
                {student.ctaLabel}
                <ChevronRight className="h-3.5 w-3.5" />
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
};
