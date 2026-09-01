import React from 'react';
import { Award, TrendingUp, Calendar, ArrowUpRight } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
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
    <div className="bg-navy-900 rounded-2xl p-4 sm:p-5 text-white relative overflow-hidden select-none">
      {/* Decorative Radial */}
      <div
        className="absolute top-0 right-0 w-36 h-36 rounded-full bg-navy-800 -mr-10 -mt-10 opacity-50 pointer-events-none"
        aria-hidden="true"
      />

      <div className="relative z-10 space-y-3 sm:space-y-4">
        {/* Top Badges */}
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-2">
            <Badge variant="default" size="sm" className="bg-primary-600 text-white font-bold">
              SON DENEME • {exam.type}
            </Badge>
            <span className="text-xs text-navy-300 flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              {exam.date}
            </span>
          </div>

          {exam.rankInInstitution && exam.totalStudents && (
            <div className="flex items-center gap-1 text-xs text-navy-200 bg-navy-800 border border-navy-700 px-2 py-0.5 rounded-md">
              <Award className="h-3.5 w-3.5 text-warning" />
              <span>Kurum Sırası: <strong className="text-white font-mono">{exam.rankInInstitution}/{exam.totalStudents}</strong></span>
            </div>
          )}
        </div>

        {/* Title and Total Net Score */}
        <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-3">
          <div>
            <h2 className="text-white font-serif text-lg sm:text-2xl leading-tight">
              {exam.title}
            </h2>
            <p className="text-navy-300 text-xs mt-1">
              Genel Sınav Değerlendirme & Net Analizi
            </p>
          </div>

          <div className="flex items-baseline gap-2 bg-navy-800/80 border border-navy-700 p-2.5 sm:px-4 sm:py-2 rounded-xl self-start sm:self-auto">
            <div>
              <span className="text-[10px] text-navy-300 block uppercase font-medium">Toplam Net</span>
              <span className="font-mono text-2xl sm:text-3xl font-bold text-white leading-none">
                {exam.totalNet.toFixed(2)}
              </span>
            </div>
            <span className="text-xs font-mono text-navy-400">/ {exam.maxTotalNet}</span>
            {exam.netChange > 0 && (
              <span className="text-xs font-mono font-semibold text-success flex items-center gap-0.5 ml-1">
                <TrendingUp className="h-3.5 w-3.5" />
                +{exam.netChange.toFixed(2)}
              </span>
            )}
          </div>
        </div>

        {/* Subject Quick Net Bar */}
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 pt-1 border-t border-navy-800">
          {exam.subjects.map((sub) => (
            <div key={sub.subjectName} className="bg-navy-950/60 rounded-xl p-2 sm:p-2.5 border border-navy-800">
              <span className="text-[11px] text-navy-300 truncate block">{sub.subjectName}</span>
              <div className="flex items-baseline justify-between mt-1">
                <span className="font-mono text-sm sm:text-base font-bold text-white">
                  {sub.net.toFixed(1)}
                </span>
                <span className="text-[10px] text-navy-400 font-mono">/{sub.maxNet}</span>
              </div>
            </div>
          ))}
        </div>

        {/* Action Button */}
        <div className="pt-1 flex items-center justify-between">
          <span className="text-xs text-navy-300 hidden sm:inline">
            Koç değerlendirmesi ve kazanım eksikleri hazır
          </span>
          <Button
            variant="subtle"
            size="sm"
            onClick={() => onViewAnalysis(exam)}
            rightIcon={<ArrowUpRight className="h-3.5 w-3.5" />}
            className="w-full sm:w-auto"
          >
            Detaylı Kazanım Analizini Gör
          </Button>
        </div>
      </div>
    </div>
  );
};
