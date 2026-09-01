import React from 'react';
import { Users, AlertTriangle, ChevronRight, CheckCircle2 } from 'lucide-react';
import { PortfolioSummaryData } from '../types';

export interface PortfolioContextCardProps {
  summary: PortfolioSummaryData;
  onNavigateToStudents: () => void;
}

export const PortfolioContextCard: React.FC<PortfolioContextCardProps> = ({
  summary,
  onNavigateToStudents,
}) => {
  return (
    <div className="space-y-3 select-none">
      <div className="flex items-center gap-2">
        <Users className="h-4 w-4 text-neutral-500" />
        <h2 className="font-semibold text-neutral-800 text-sm">Portföy Özeti</h2>
      </div>

      <div className="bg-white rounded-2xl border border-neutral-100 p-4 space-y-3 shadow-soft-sm">
        {/* Plan adherence */}
        <div className="space-y-1">
          <div className="flex items-center justify-between text-xs">
            <span className="text-neutral-500">Plan Uyumu Ort.</span>
            <span className="font-mono text-sm font-semibold text-neutral-800">
              %{summary.planAdherenceAvg}
            </span>
          </div>
          <div className="h-1.5 bg-neutral-100 rounded-full overflow-hidden">
            <div
              className="h-full bg-success rounded-full transition-all"
              style={{ width: `${summary.planAdherenceAvg}%` }}
            />
          </div>
        </div>

        {/* Study hours */}
        <div className="space-y-1 pt-1">
          <div className="flex items-center justify-between text-xs">
            <span className="text-neutral-500">Haftalık Çalışma Ort.</span>
            <span className="font-mono text-sm font-semibold text-neutral-800">
              {summary.weeklyStudyHoursAvg} sa
            </span>
          </div>
          <div className="h-1.5 bg-neutral-100 rounded-full overflow-hidden">
            <div
              className="h-full bg-primary-500 rounded-full transition-all"
              style={{ width: `${Math.min(100, (summary.weeklyStudyHoursAvg / 25) * 100)}%` }}
            />
          </div>
        </div>

        {/* Count stats */}
        <div className="pt-2 mt-1 border-t border-neutral-100 space-y-2">
          <div className="flex items-center justify-between text-xs">
            <div className="flex items-center gap-1.5 text-neutral-500">
              <CheckCircle2 className="h-3.5 w-3.5 text-success" />
              <span>Aktif Öğrenci</span>
            </div>
            <span className="font-mono text-sm font-semibold text-neutral-800">
              {summary.activeStudentsCount}
            </span>
          </div>

          <div className="flex items-center justify-between text-xs">
            <div className="flex items-center gap-1.5 text-neutral-500">
              <AlertTriangle className="h-3.5 w-3.5 text-danger" />
              <span>Dikkat Gerektiren</span>
            </div>
            <span className="font-mono text-sm font-semibold text-danger">
              {summary.attentionRequiredCount}
            </span>
          </div>
        </div>

        <button
          type="button"
          onClick={onNavigateToStudents}
          className="w-full mt-2 text-xs font-semibold text-primary-600 hover:text-primary-700 py-2.5 flex items-center justify-center gap-1 border border-neutral-100 rounded-xl hover:border-primary-200 hover:bg-primary-50 transition-colors min-h-[38px]"
        >
          Öğrenci Listesine Git <ChevronRight className="h-3.5 w-3.5" />
        </button>
      </div>
    </div>
  );
};
