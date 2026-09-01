import React from 'react';
import { Calendar, ChevronRight, TrendingUp } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { StudentExamItem } from '../types';

export interface ExamListItemCardProps {
  exam: StudentExamItem;
  onClick: (exam: StudentExamItem) => void;
}

export const ExamListItemCard: React.FC<ExamListItemCardProps> = ({ exam, onClick }) => {
  return (
    <div
      onClick={() => onClick(exam)}
      className="bg-white rounded-2xl border border-neutral-100 hover:border-neutral-200 hover:shadow-soft-sm transition-all p-4 cursor-pointer select-none"
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        {/* Left Info */}
        <div className="space-y-1 min-w-0 flex-1">
          <div className="flex items-center gap-2">
            <Badge variant={exam.type === 'TYT' ? 'info' : 'attention'} size="sm">
              {exam.type}
            </Badge>
            <span className="text-xs text-neutral-400 flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              {exam.date}
            </span>
          </div>

          <h4 className="text-sm sm:text-base font-semibold text-neutral-900 leading-tight truncate">
            {exam.title}
          </h4>

          {exam.focusWeakTopics && exam.focusWeakTopics.length > 0 && (
            <p className="text-xs text-neutral-400 truncate">
              Odak Konular: {exam.focusWeakTopics.join(', ')}
            </p>
          )}
        </div>

        {/* Right Net & Comparison */}
        <div className="flex items-center justify-between sm:justify-end gap-4 border-t sm:border-t-0 pt-2 sm:pt-0 border-neutral-50">
          <div className="text-left sm:text-right">
            <div className="flex items-baseline gap-1 sm:justify-end">
              <span className="font-mono text-xl font-bold text-neutral-900">
                {exam.totalNet.toFixed(2)}
              </span>
              <span className="text-xs font-mono text-neutral-400">Net</span>
            </div>

            {exam.netChange !== 0 && (
              <span
                className={`text-[11px] font-mono font-medium flex items-center gap-0.5 sm:justify-end ${
                  exam.netChange > 0 ? 'text-success' : 'text-danger'
                }`}
              >
                {exam.netChange > 0 ? <TrendingUp className="h-3 w-3" /> : null}
                {exam.netChange > 0 ? `+${exam.netChange.toFixed(2)}` : exam.netChange.toFixed(2)}
              </span>
            )}
          </div>

          <div className="w-8 h-8 rounded-full bg-neutral-50 flex items-center justify-center text-neutral-400 group-hover:text-primary-600">
            <ChevronRight className="h-4 w-4" />
          </div>
        </div>
      </div>
    </div>
  );
};
