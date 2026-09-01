import React from 'react';
import { Users, ChevronRight, AlertTriangle, BookOpen } from 'lucide-react';
import { TeacherClassDetail } from '../types';

export interface ClassCardProps {
  cls: TeacherClassDetail;
  onSelect: (classId: string) => void;
}

export const ClassCard: React.FC<ClassCardProps> = ({ cls, onSelect }) => {
  return (
    <div
      onClick={() => onSelect(cls.id)}
      className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm hover:shadow-soft-md transition-all cursor-pointer select-none group flex flex-col justify-between"
    >
      <div className="space-y-3">
        {/* Top bar: Class Name & Grade */}
        <div className="flex items-start justify-between gap-2">
          <div>
            <h3 className="font-semibold text-base sm:text-lg text-neutral-900 group-hover:text-primary-700 transition-colors">
              {cls.name}
            </h3>
            <p className="text-xs text-neutral-400 mt-0.5">
              {cls.grade} • {cls.branch}
            </p>
          </div>

          <div className="flex items-center gap-1 text-xs text-neutral-500 bg-surface-alt px-2.5 py-1 rounded-xl border border-neutral-200">
            <Users className="h-3.5 w-3.5" />
            <span className="font-mono font-bold">{cls.studentCount}</span>
            <span>Öğrenci</span>
          </div>
        </div>

        {/* Assigned Subjects */}
        <div className="flex items-center gap-1.5 text-xs text-neutral-600">
          <BookOpen className="h-3.5 w-3.5 text-primary-600 flex-shrink-0" />
          <span className="font-medium">{cls.subject}</span>
        </div>

        {/* Stats Row */}
        <div className="grid grid-cols-2 gap-2 pt-2 border-t border-neutral-100">
          <div className="bg-surface-alt rounded-xl p-2.5">
            <span className="text-[10px] uppercase tracking-wider text-neutral-400 font-semibold block">
              Sınıf Net Ortalaması
            </span>
            <span className="font-mono text-base font-bold text-neutral-900 mt-0.5 block">
              {cls.averageNet} net
            </span>
          </div>

          <div className="bg-surface-alt rounded-xl p-2.5">
            <span className="text-[10px] uppercase tracking-wider text-neutral-400 font-semibold block">
              Akademik Takip
            </span>
            {cls.attentionRequiredCount > 0 ? (
              <div className="flex items-center gap-1 text-danger mt-0.5">
                <AlertTriangle className="h-3.5 w-3.5" />
                <span className="font-mono text-xs font-bold">
                  {cls.attentionRequiredCount} Öğrenci
                </span>
              </div>
            ) : (
              <span className="text-xs font-medium text-success mt-0.5 block">
                Her şey yolunda
              </span>
            )}
          </div>
        </div>
      </div>

      {/* Bottom CTA */}
      <div className="pt-3 mt-3 border-t border-neutral-100 flex items-center justify-between text-xs text-primary-700 font-semibold">
        <span>Sınıf Analizi & Öğrenci Listesi</span>
        <ChevronRight className="h-4 w-4 transform group-hover:translate-x-1 transition-transform" />
      </div>
    </div>
  );
};
