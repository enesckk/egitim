import React from 'react';
import { Users, ChevronRight, BookOpen, User, AlertCircle } from 'lucide-react';
import { TeacherClassDetail } from '../types';

export interface ClassCardProps {
  cls: TeacherClassDetail;
  onSelect: (classId: string) => void;
}

export const ClassCard: React.FC<ClassCardProps> = ({ cls, onSelect }) => {
  return (
    <div
      onClick={() => onSelect(cls.id)}
      className="w-full bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-sm hover:shadow-soft-md hover:border-primary-300 transition-all cursor-pointer select-none group flex flex-col justify-between"
      data-testid={`class-card-${cls.id}`}
    >
      <div className="space-y-3.5 sm:space-y-4">
        {/* Top Header: Class Name, Grade & Student Count */}
        <div className="flex items-start justify-between gap-2.5">
          <div className="min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h3 className="font-semibold text-base sm:text-lg text-neutral-900 group-hover:text-primary-700 transition-colors truncate">
                {cls.name}
              </h3>
              <span className="inline-flex items-center text-[11px] font-bold px-2 py-0.5 rounded-full bg-primary-50 text-primary-700 border border-primary-100">
                {cls.grade}
              </span>
            </div>
            <p className="text-xs text-neutral-500 mt-1">
              Şube: <span className="font-medium text-neutral-700">{cls.branch}</span>
            </p>
          </div>

          <div className="flex items-center gap-1.5 text-xs text-neutral-700 bg-[#F8F7F3] px-2.5 sm:px-3 py-1.5 rounded-xl border border-neutral-200/80 flex-shrink-0">
            <Users className="h-3.5 w-3.5 text-neutral-500" />
            <span className="font-mono font-bold text-neutral-900">{cls.studentCount}</span>
            <span className="text-[11px] text-neutral-500">Öğrenci</span>
          </div>
        </div>

        {/* Assigned Teacher & Subject */}
        <div className="space-y-1.5 bg-neutral-50/70 p-2.5 rounded-xl border border-neutral-100 text-xs text-neutral-600">
          <div className="flex items-center gap-2">
            <BookOpen className="h-3.5 w-3.5 text-primary-600 flex-shrink-0" />
            <span className="truncate">
              <strong className="text-neutral-700 font-medium">Branş:</strong> {cls.subject}
            </span>
          </div>
          {cls.teacherName && (
            <div className="flex items-center gap-2 text-neutral-500 text-[11px]">
              <User className="h-3.5 w-3.5 text-neutral-400 flex-shrink-0" />
              <span className="truncate">
                <strong className="text-neutral-600 font-medium">Öğretmen:</strong> {cls.teacherName}
              </span>
            </div>
          )}
        </div>

        {/* Academic Status Row */}
        <div className="grid grid-cols-2 gap-2 sm:gap-2.5 pt-1">
          <div className="bg-[#F8F7F3] rounded-xl p-2.5 sm:p-3 border border-neutral-200/60">
            <span className="text-[10px] uppercase tracking-wider text-neutral-500 font-semibold block">
              Sınıf Net Ortalaması
            </span>
            <span className="text-xs font-semibold text-neutral-800 mt-1 block truncate">
              {cls.averageNet != null ? `${cls.averageNet} net` : 'Henüz sınav verisi yok'}
            </span>
          </div>

          <div className="bg-[#F8F7F3] rounded-xl p-2.5 sm:p-3 border border-neutral-200/60">
            <span className="text-[10px] uppercase tracking-wider text-neutral-500 font-semibold block">
              Akademik Takip
            </span>
            {cls.attentionRequiredCount > 0 ? (
              <div className="flex items-center gap-1.5 text-amber-600 mt-1">
                <AlertCircle className="h-3.5 w-3.5 flex-shrink-0" />
                <span className="text-xs font-bold">
                  {cls.attentionRequiredCount} Öğrenci
                </span>
              </div>
            ) : (
              <span className="text-xs font-medium text-neutral-600 mt-1 block">
                Veri Bekleniyor
              </span>
            )}
          </div>
        </div>
      </div>

      {/* Bottom CTA */}
      <div className="pt-3 mt-3.5 border-t border-neutral-100 flex items-center justify-between text-xs text-primary-700 font-semibold group-hover:text-primary-800">
        <span>Sınıf Analizi & Öğrenci Listesi</span>
        <ChevronRight className="h-4 w-4 transform group-hover:translate-x-1 transition-transform" />
      </div>
    </div>
  );
};
