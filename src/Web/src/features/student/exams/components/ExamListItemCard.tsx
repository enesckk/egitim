import React from 'react';
import { Calendar, ChevronRight, TrendingUp, TrendingDown } from 'lucide-react';
import { StudentExamItem } from '../types';

export interface ExamListItemCardProps {
  exam: StudentExamItem;
  onClick: (exam: StudentExamItem) => void;
}

export const ExamListItemCard: React.FC<ExamListItemCardProps> = ({ exam, onClick }) => {
  const shortId = exam.attemptId ? exam.attemptId.slice(-4).toUpperCase() : null;

  return (
    <div
      onClick={() => onClick(exam)}
      data-testid="exam-list-item"
      className="bg-white rounded-2xl border border-neutral-200/80 hover:border-navy-200 hover:shadow-soft-md transition-all p-4 sm:p-5 cursor-pointer select-none group"
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 sm:gap-4">
        {/* Left: Type Badge, Title, Date, Grounded Attempt ID */}
        <div className="space-y-1.5 min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2">
            <span
              className={`text-[11px] font-bold px-2 py-0.5 rounded-md uppercase tracking-wider ${
                exam.type === 'TYT'
                  ? 'bg-blue-50 text-blue-700 border border-blue-100'
                  : exam.type === 'AYT'
                  ? 'bg-purple-50 text-purple-700 border border-purple-100'
                  : 'bg-emerald-50 text-emerald-700 border border-emerald-100'
              }`}
            >
              {exam.type}
            </span>

            {shortId && (
              <span className="text-[10px] font-mono text-neutral-400 bg-neutral-100/80 px-1.5 py-0.5 rounded border border-neutral-200/50">
                #{shortId}
              </span>
            )}

            <span className="text-xs text-neutral-500 flex items-center gap-1 font-medium">
              <Calendar className="h-3 w-3 text-neutral-400" />
              {exam.date}
            </span>
          </div>

          <h4 className="text-sm sm:text-base font-semibold text-neutral-900 leading-snug group-hover:text-primary-700 transition-colors truncate">
            {exam.title}
          </h4>

          {exam.focusWeakTopics && exam.focusWeakTopics.length > 0 && (
            <p className="text-xs text-neutral-500 truncate">
              Geliştirilecek: <span className="text-neutral-700 font-medium">{exam.focusWeakTopics.slice(0, 3).join(', ')}</span>
            </p>
          )}
        </div>

        {/* Right: Net Score & Delta & Chevron */}
        <div className="flex items-center justify-between sm:justify-end gap-3 sm:gap-5 border-t sm:border-t-0 pt-2.5 sm:pt-0 border-neutral-100">
          <div className="text-left sm:text-right">
            <div className="flex items-baseline gap-1 sm:justify-end">
              <span className="font-mono text-xl sm:text-2xl font-bold text-neutral-900">
                {exam.totalNet.toFixed(2)}
              </span>
              <span className="text-xs font-semibold text-neutral-400">Net</span>
            </div>

            {exam.netChange !== undefined && exam.netChange !== 0 && (
              <span
                className={`text-[11px] font-mono font-semibold inline-flex items-center gap-0.5 sm:justify-end ${
                  exam.netChange > 0 ? 'text-emerald-600' : 'text-rose-600'
                }`}
              >
                {exam.netChange > 0 ? (
                  <TrendingUp className="h-3 w-3" />
                ) : (
                  <TrendingDown className="h-3 w-3" />
                )}
                {exam.netChange > 0 ? `+${exam.netChange.toFixed(2)}` : exam.netChange.toFixed(2)} Net
              </span>
            )}
          </div>

          <div className="w-8 h-8 rounded-full bg-neutral-50 group-hover:bg-primary-50 flex items-center justify-center text-neutral-400 group-hover:text-primary-600 transition-colors shrink-0">
            <ChevronRight className="h-4 w-4" />
          </div>
        </div>
      </div>
    </div>
  );
};
