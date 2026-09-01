import React from 'react';
import { Award, Calendar, TrendingUp, AlertTriangle } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { EmptyState } from '@/components/ui/EmptyState';
import { StudentExamReviewItem } from '../types';

export interface StudentExamsTabProps {
  exams: StudentExamReviewItem[];
  studentName: string;
}

export const StudentExamsTab: React.FC<StudentExamsTabProps> = ({ exams }) => {
  return (
    <div className="space-y-4 select-none">
      {exams.length > 0 ? (
        <div className="space-y-3">
          {exams.map((exam) => (
            <div
              key={exam.id}
              className="bg-white rounded-2xl border border-neutral-100 p-4 space-y-3 shadow-soft-sm"
            >
              {/* Header */}
              <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <Badge variant={exam.type === 'TYT' ? 'info' : 'attention'} size="sm">
                    {exam.type}
                  </Badge>
                  <h4 className="text-sm sm:text-base font-semibold text-neutral-900">
                    {exam.title}
                  </h4>
                </div>

                <span className="text-xs text-neutral-400 flex items-center gap-1 font-mono">
                  <Calendar className="h-3 w-3" />
                  {exam.date}
                </span>
              </div>

              {/* Net Score & Subjects Summary */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5 pt-1">
                <div className="bg-surface-alt rounded-xl p-2.5 flex items-baseline justify-between">
                  <span className="text-xs text-neutral-500">Toplam Net</span>
                  <div className="flex items-baseline gap-1.5">
                    <span className="font-mono text-lg font-bold text-neutral-900">
                      {exam.totalNet.toFixed(2)}
                    </span>
                    {exam.netChange > 0 && (
                      <span className="text-xs font-mono font-semibold text-success flex items-center">
                        <TrendingUp className="h-3 w-3 mr-0.5" />
                        +{exam.netChange.toFixed(2)}
                      </span>
                    )}
                  </div>
                </div>

                <div className="bg-surface-alt rounded-xl p-2.5 sm:col-span-2 flex flex-col justify-center">
                  <span className="text-[10px] text-neutral-400 block uppercase">Ders Netleri</span>
                  <p className="font-mono text-xs font-semibold text-neutral-800 truncate mt-0.5">
                    {exam.subjectsSummary}
                  </p>
                </div>
              </div>

              {/* Weak Topics */}
              {exam.weakTopics && exam.weakTopics.length > 0 && (
                <div className="bg-amber-50/50 border border-amber-200 rounded-xl p-2.5 flex flex-wrap items-center gap-1.5">
                  <span className="text-xs font-semibold text-amber-900 flex items-center gap-1">
                    <AlertTriangle className="h-3 w-3 text-warning" />
                    Öncelikli Kazanım Eksikleri:
                  </span>
                  {exam.weakTopics.map((topic) => (
                    <span
                      key={topic}
                      className="text-[11px] bg-white border border-amber-200 text-amber-900 px-2 py-0.5 rounded-md font-medium"
                    >
                      {topic}
                    </span>
                  ))}
                </div>
              )}
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<Award className="h-6 w-6 text-neutral-400" />}
          title="Deneme Sınavı Kaydı Yok"
          description="Öğrencinin henüz sistemde kayıtlı deneme sınav sonucu bulunmuyor."
        />
      )}
    </div>
  );
};
