import React from 'react';
import { AlertTriangle, ChevronRight, BookOpen, AlertCircle } from 'lucide-react';
import { AcademicGapItem, StudentAttentionGroup } from '../types';

export interface AcademicGapsCardProps {
  gaps: AcademicGapItem[];
  onSelectStudent: (studentId: string) => void;
}

export const AcademicGapsCard: React.FC<AcademicGapsCardProps> = ({
  gaps,
  onSelectStudent,
}) => {
  const criticalCount = gaps.filter((g) => g.severity === 'critical').length;
  const totalCount = gaps.length;

  // Group academic gaps by student (1 card per student)
  const studentGroups: StudentAttentionGroup[] = React.useMemo(() => {
    const map = new Map<string, StudentAttentionGroup>();

    gaps.forEach((gap) => {
      let group = map.get(gap.studentId);
      if (!group) {
        group = {
          studentId: gap.studentId,
          studentName: gap.studentName,
          initials: gap.initials,
          grade: gap.grade,
          criticalCount: 0,
          totalCount: 0,
          gaps: [],
          topTopics: [],
        };
        map.set(gap.studentId, group);
      }

      group.totalCount += 1;
      if (gap.severity === 'critical') {
        group.criticalCount += 1;
      }
      group.gaps.push(gap);
      if (!group.topTopics.includes(gap.topic)) {
        group.topTopics.push(gap.topic);
      }
    });

    // Sort students by critical gaps descending, then total gaps descending
    return Array.from(map.values()).sort((a, b) => {
      if (b.criticalCount !== a.criticalCount) {
        return b.criticalCount - a.criticalCount;
      }
      return b.totalCount - a.totalCount;
    });
  }, [gaps]);

  return (
    <div className="space-y-3.5 select-none" data-testid="attention-students-section">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <div className="p-1.5 rounded-lg bg-red-50 text-red-600">
            <AlertTriangle className="h-4 w-4" />
          </div>
          <div>
            <h2 className="font-bold text-[#14233E] text-base sm:text-lg">
              Dikkat Gerektiren Öğrenciler
            </h2>
            <p className="text-xs text-neutral-500">
              Şubelerinizde konu ve kazanım açığı tespit edilen öğrenciler
            </p>
          </div>
        </div>

        {totalCount > 0 && (
          <span
            className="inline-flex items-center justify-center px-2.5 py-1 rounded-full bg-red-100 text-red-800 text-xs font-bold font-mono"
            title={`${criticalCount} kritik konu açığı, toplam ${totalCount} konu açığı tespit edildi`}
          >
            {criticalCount} Kritik Konu Açığı / {totalCount} Toplam Açık
          </span>
        )}
      </div>

      {studentGroups.length === 0 ? (
        <div className="p-8 text-center bg-white rounded-2xl border border-neutral-200/70 text-neutral-500 space-y-2">
          <div className="w-10 h-10 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center mx-auto">
            <BookOpen className="h-5 w-5" />
          </div>
          <p className="text-sm font-semibold text-[#14233E]">Akademik Açık Tespit Edilmedi</p>
          <p className="text-xs text-neutral-400">Atanmış şubelerinizdeki öğrenciler için henüz kritik veya öncelikli bir konu açığı bulunmuyor.</p>
        </div>
      ) : (
        <div className="space-y-3">
          {studentGroups.map((group) => {
            const hasCritical = group.criticalCount > 0;

            return (
              <div
                key={group.studentId}
                onClick={() => onSelectStudent(group.studentId)}
                className="bg-white rounded-2xl border border-neutral-200/70 p-4 sm:p-5 hover:border-[#315FE8]/40 hover:shadow-sm transition-all cursor-pointer group flex flex-col sm:flex-row sm:items-center justify-between gap-3.5"
                data-testid="student-attention-card"
              >
                <div className="flex items-start gap-3.5 min-w-0">
                  <div className="w-11 h-11 rounded-xl bg-[#14233E] text-white text-sm font-bold flex items-center justify-center flex-shrink-0 shadow-xs">
                    {group.initials}
                  </div>

                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <p className="font-bold text-sm sm:text-base text-[#14233E] group-hover:text-[#315FE8] transition-colors truncate">
                        {group.studentName}
                      </p>
                      <span className="text-[11px] font-medium px-2 py-0.5 rounded-md bg-neutral-100 text-neutral-600">
                        {group.grade}
                      </span>
                      {hasCritical ? (
                        <span className="inline-flex items-center gap-1 text-[11px] font-bold px-2 py-0.5 rounded-full bg-red-50 text-red-700 border border-red-200">
                          <AlertCircle className="h-3 w-3" />
                          {group.criticalCount} Kritik Açık
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 text-[11px] font-medium px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200">
                          {group.totalCount} Konu Takipte
                        </span>
                      )}
                    </div>

                    {/* Top priority topics */}
                    <div className="flex items-center gap-1.5 flex-wrap mt-2">
                      <span className="text-[11px] font-medium text-neutral-500">Öncelikli Konular:</span>
                      {group.topTopics.slice(0, 3).map((topic) => (
                        <span
                          key={topic}
                          className="text-[11px] font-medium px-2 py-0.5 rounded-md bg-[#F8F7F3] text-[#14233E] border border-neutral-200/60"
                        >
                          {topic}
                        </span>
                      ))}
                      {group.topTopics.length > 3 && (
                        <span className="text-[11px] text-neutral-400 font-medium">
                          +{group.topTopics.length - 3} diğer
                        </span>
                      )}
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-2 self-end sm:self-center flex-shrink-0 pt-2 sm:pt-0 border-t sm:border-t-0 border-neutral-100 w-full sm:w-auto justify-between sm:justify-end">
                  <span className="text-xs font-semibold text-[#315FE8] group-hover:underline">
                    Öğrenci Karnesi
                  </span>
                  <ChevronRight className="h-4 w-4 text-neutral-400 group-hover:text-[#315FE8] group-hover:translate-x-0.5 transition-all" />
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
