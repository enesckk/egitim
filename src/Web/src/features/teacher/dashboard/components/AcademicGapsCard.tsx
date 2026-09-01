import React from 'react';
import { AlertTriangle, TrendingUp, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { AcademicGapItem, AcademicSeverity } from '../types';

export interface AcademicGapsCardProps {
  gaps: AcademicGapItem[];
  onSelectStudent: (studentId: string) => void;
}

const SEVERITY_CONFIG: Record<
  AcademicSeverity,
  { bg: string; border: string; icon: React.ReactNode; badgeClass: string; label: string }
> = {
  critical: {
    bg: 'bg-danger-light',
    border: 'border-red-200',
    icon: <AlertTriangle className="h-4 w-4 text-danger flex-shrink-0" />,
    badgeClass: 'bg-danger text-white',
    label: 'Kritik',
  },
  warning: {
    bg: 'bg-warning-light',
    border: 'border-amber-200',
    icon: <AlertTriangle className="h-4 w-4 text-warning flex-shrink-0" />,
    badgeClass: 'bg-warning text-white',
    label: 'Dikkat',
  },
  info: {
    bg: 'bg-primary-50',
    border: 'border-primary-100',
    icon: <TrendingUp className="h-4 w-4 text-primary-600 flex-shrink-0" />,
    badgeClass: 'bg-primary-100 text-primary-700',
    label: 'Takipte',
  },
};

export const AcademicGapsCard: React.FC<AcademicGapsCardProps> = ({
  gaps,
  onSelectStudent,
}) => {
  const criticalCount = gaps.filter((g) => g.severity === 'critical').length;

  return (
    <div className="space-y-3 select-none">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 text-danger" />
          <h2 className="font-semibold text-neutral-800 text-sm sm:text-base">
            Öne Çıkan Akademik Açıklar
          </h2>
          {criticalCount > 0 && (
            <span className="inline-flex items-center justify-center w-5 h-5 rounded-full bg-danger text-white text-xs font-bold font-mono">
              {criticalCount}
            </span>
          )}
        </div>
      </div>

      <div className="space-y-2.5">
        {gaps.map((gap) => {
          const style = SEVERITY_CONFIG[gap.severity];

          return (
            <div
              key={gap.id}
              className={cn(
                'rounded-2xl border p-3.5 sm:p-4 flex flex-col sm:flex-row sm:items-start justify-between gap-3 transition-all',
                style.bg,
                style.border
              )}
            >
              <div className="flex items-start gap-3 min-w-0">
                <div className="w-10 h-10 rounded-2xl bg-white border border-neutral-200 text-neutral-800 text-xs font-bold flex items-center justify-center flex-shrink-0 shadow-soft-sm">
                  {gap.initials}
                </div>

                <div className="min-w-0 flex-1">
                  <div className="flex items-center gap-2 flex-wrap mb-1">
                    <p className="font-semibold text-sm text-neutral-900">
                      {gap.studentName}
                    </p>
                    <span className="text-xs text-neutral-400">
                      {gap.grade}
                    </span>
                    <span className="text-xs font-medium text-neutral-700 bg-white/80 px-2.5 py-0.5 rounded-full border border-neutral-200 shadow-soft-sm">
                      {gap.subject} — {gap.topic}
                    </span>
                  </div>

                  <div className="flex items-start gap-1.5 mt-1 text-xs text-neutral-600 leading-relaxed">
                    {style.icon}
                    <span>{gap.detail}</span>
                  </div>

                  <p className="text-[11px] text-neutral-400 mt-1.5 font-mono">
                    Son deneme: {gap.lastExam}
                  </p>
                </div>
              </div>

              <button
                type="button"
                onClick={() => onSelectStudent(gap.studentId)}
                className="self-end sm:self-center flex-shrink-0 flex items-center gap-1 text-xs font-semibold text-primary-700 bg-white border border-primary-200 px-3 py-1.5 rounded-xl hover:bg-primary-50 transition-colors min-h-[32px] shadow-soft-sm whitespace-nowrap"
              >
                <span>Öğrenciye Git</span>
                <ChevronRight className="h-3.5 w-3.5" />
              </button>
            </div>
          );
        })}
      </div>
    </div>
  );
};
