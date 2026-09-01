import React from 'react';
import { AlertTriangle, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { AdminCoachPortfolioItem } from '../types';

export interface CoachPortfolioCardProps {
  coaches: AdminCoachPortfolioItem[];
  timeframeLabel: string;
  onSelectCoach?: (coachId: string) => void;
}

export const CoachPortfolioCard: React.FC<CoachPortfolioCardProps> = ({
  coaches,
  timeframeLabel,
  onSelectCoach,
}) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-100 overflow-hidden shadow-soft-sm select-none">
      <div className="flex items-center justify-between px-5 py-4 border-b border-neutral-100">
        <div>
          <h2 className="font-semibold text-neutral-800 text-sm">Koç Portföyü</h2>
          <p className="text-xs text-neutral-400 mt-0.5">
            Plan uyumu ve öğrenci sayısı — {timeframeLabel}
          </p>
        </div>
      </div>

      <div className="divide-y divide-neutral-50">
        {coaches.map((coach) => (
          <div
            key={coach.id}
            onClick={() => onSelectCoach?.(coach.id)}
            className="flex items-center gap-4 px-5 py-3.5 hover:bg-neutral-50/80 transition-colors cursor-pointer group"
          >
            <div className="w-9 h-9 rounded-xl bg-navy-50 text-navy-800 text-xs font-bold flex items-center justify-center flex-shrink-0">
              {coach.initials}
            </div>

            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 mb-0.5">
                <p className="text-sm font-semibold text-neutral-800 truncate">{coach.name}</p>
                {coach.status === 'attention' && (
                  <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-warning-dark bg-warning-light border border-amber-200 px-2 py-0.5 rounded-full">
                    <AlertTriangle className="h-3 w-3 text-warning" />
                    Dikkat
                  </span>
                )}
              </div>
              <p className="text-xs text-neutral-500 truncate">
                Portföy — Ortalama Plan Uyumu{' '}
                <strong
                  className={cn(
                    'font-mono font-bold',
                    coach.adherence >= 80
                      ? 'text-success'
                      : coach.adherence >= 70
                      ? 'text-warning-dark'
                      : 'text-danger'
                  )}
                >
                  %{coach.adherence}
                </strong>{' '}
                · {coach.students} öğrenci · Ort. {coach.avgStudy} sa/hafta
              </p>
            </div>

            <div className="flex items-center gap-3 flex-shrink-0">
              {coach.attention > 0 && (
                <div className="text-right hidden sm:block">
                  <p className="text-xs font-bold text-danger font-mono">{coach.attention}</p>
                  <p className="text-[10px] text-neutral-400">dikkat</p>
                </div>
              )}
              <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-neutral-500 transition-colors" />
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
