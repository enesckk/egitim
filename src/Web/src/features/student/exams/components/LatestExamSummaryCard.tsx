import React from 'react';
import { Award, TrendingUp, Calendar, ChevronRight } from 'lucide-react';
import { StudentExamItem } from '../types';

export interface LatestExamSummaryCardProps {
  exam: StudentExamItem;
  onViewAnalysis: (exam: StudentExamItem) => void;
}

export const LatestExamSummaryCard: React.FC<LatestExamSummaryCardProps> = ({
  exam,
  onViewAnalysis,
}) => {
  return (
    <div
      data-testid="latest-exam-hero"
      className="bg-[#14233E] rounded-2xl p-5 sm:p-6 text-white relative overflow-hidden shadow-xs select-none"
    >
      {/* Subtle background glow */}
      <div
        className="absolute -top-12 -right-12 w-36 h-36 bg-[#315FE8]/25 rounded-full blur-2xl pointer-events-none"
        aria-hidden="true"
      />

      <div className="relative z-10 space-y-4">
        {/* Top Badges */}
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <span className="px-2.5 py-0.5 rounded-full text-[10px] font-bold bg-[#315FE8] text-white">
              SON DENEME • {exam.type}
            </span>
            <span className="text-xs text-blue-200 flex items-center gap-1 font-medium">
              <Calendar className="h-3 w-3 text-blue-300" />
              {exam.date}
            </span>
          </div>

          {exam.rankInInstitution && exam.totalStudents ? (
            <div className="flex items-center gap-1.5 text-xs text-blue-100 bg-white/10 px-2.5 py-1 rounded-full border border-white/10">
              <Award className="h-3.5 w-3.5 text-amber-300" />
              <span>Kurum Sırası: <strong className="text-white font-mono">{exam.rankInInstitution}/{exam.totalStudents}</strong></span>
            </div>
          ) : null}
        </div>

        {/* Title and Total Net Score */}
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-3">
          <div>
            <h2 className="text-white text-lg sm:text-2xl font-bold tracking-tight leading-tight">
              {exam.title}
            </h2>
            <p className="text-blue-200/80 text-xs mt-1">
              Genel Sınav Karnesi & Net Başarımı
            </p>
          </div>

          <div className="flex items-baseline gap-2 bg-white/10 border border-white/10 p-2.5 sm:px-4 sm:py-2.5 rounded-2xl self-start sm:self-auto">
            <div>
              <span className="text-[10px] text-blue-200 block uppercase font-medium">Toplam Net</span>
              <span className="text-2xl sm:text-3xl font-bold text-white leading-none">
                {Math.round(exam.totalNet * 100) / 100}
              </span>
            </div>
            <span className="text-xs text-blue-300 font-medium">net</span>
            {exam.netChange !== undefined && exam.netChange !== 0 && (
              <span
                className={`text-xs font-semibold flex items-center gap-0.5 ml-1.5 ${
                  exam.netChange > 0 ? 'text-emerald-300' : 'text-rose-300'
                }`}
              >
                {exam.netChange > 0 ? <TrendingUp className="h-3.5 w-3.5" /> : null}
                {exam.netChange > 0 ? `+${exam.netChange}` : exam.netChange}
              </span>
            )}
          </div>
        </div>

        {/* Subject Quick Net Bar */}
        {exam.subjects && exam.subjects.length > 0 && (
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-2 border-t border-white/10">
            {exam.subjects.map((sub) => (
              <div key={sub.subjectName} className="bg-white/5 rounded-xl p-2.5">
                <span className="text-[11px] text-blue-200/90 font-medium block truncate">
                  {sub.subjectName}
                </span>
                <span className="text-sm font-bold text-white mt-0.5 block">
                  {sub.net} net
                </span>
                <span className="text-[10px] text-blue-300/70 block mt-0.5">
                  {sub.correct}D {sub.wrong}Y
                </span>
              </div>
            ))}
          </div>
        )}

        {/* Action Button */}
        <div className="flex justify-end pt-1">
          <button
            type="button"
            onClick={() => onViewAnalysis(exam)}
            className="inline-flex items-center gap-1.5 px-4 py-2 rounded-full bg-white/15 hover:bg-white/25 active:scale-95 text-white text-xs font-semibold transition-all cursor-pointer"
          >
            Sonuç Karnesini İncele
            <ChevronRight className="w-3.5 h-3.5" />
          </button>
        </div>
      </div>
    </div>
  );
};
