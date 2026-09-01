import React from 'react';
import { Zap, Play } from 'lucide-react';
import { NextStudyItem } from '../types';

export interface NextStudyCardProps {
  nextStudy: NextStudyItem;
  completedCount: number;
  totalCount: number;
  onStartStudy?: () => void;
}

export const NextStudyCard: React.FC<NextStudyCardProps> = ({
  nextStudy,
  completedCount,
  totalCount,
  onStartStudy,
}) => {
  return (
    <div className="bg-navy-900 rounded-2xl p-4 sm:p-5 relative overflow-hidden select-none">
      {/* Decorative radial overlay */}
      <div
        className="absolute top-0 right-0 w-32 h-32 rounded-full bg-navy-800 -mr-8 -mt-8 opacity-60 pointer-events-none"
        aria-hidden="true"
      />

      <div className="relative z-10">
        <div className="flex items-center gap-1.5 sm:gap-2 mb-2 sm:mb-3">
          <Zap className="h-3.5 w-3.5 text-navy-300" />
          <span className="text-navy-300 text-[11px] sm:text-xs font-medium tracking-wide uppercase">
            Sıradaki Çalışma
          </span>
        </div>

        <h2 className="text-white font-semibold text-base sm:text-lg leading-tight mb-0.5">
          {nextStudy.title}
        </h2>
        <p className="text-navy-200 text-xs sm:text-sm mb-3 sm:mb-4">
          {nextStudy.topic} • {nextStudy.durationMinutes} dk
        </p>

        <div className="flex items-center justify-between sm:justify-start gap-2.5 sm:gap-4">
          <button
            type="button"
            onClick={onStartStudy}
            className="inline-flex items-center gap-1.5 sm:gap-2 bg-primary-600 hover:bg-primary-500 active:bg-primary-700 text-white text-xs sm:text-sm font-semibold px-4 py-2 sm:px-5 sm:py-2.5 rounded-xl transition-colors min-h-[40px] sm:min-h-[44px] whitespace-nowrap shadow-soft-sm focus:outline-none focus:ring-2 focus:ring-primary-500 focus:ring-offset-2 focus:ring-offset-navy-900"
          >
            <Play className="h-3 w-3 sm:h-3.5 sm:w-3.5 fill-current" />
            Çalışmaya Başla
          </button>

          <span className="text-navy-300 text-[11px] sm:text-xs font-medium whitespace-nowrap text-right">
            Bugün {completedCount}/{totalCount} tamamlandı
          </span>
        </div>
      </div>
    </div>
  );
};
