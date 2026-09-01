import React from 'react';
import { LinkedStudentSummary } from '../types';

export interface ChildHeroCardProps {
  student: LinkedStudentSummary;
}

export const ChildHeroCard: React.FC<ChildHeroCardProps> = ({ student }) => {
  return (
    <div className="bg-navy-900 rounded-2xl p-5 shadow-soft-sm text-white select-none">
      <div className="flex items-center gap-4">
        <div className="w-12 h-12 rounded-full bg-navy-700 text-white text-base font-semibold flex items-center justify-center flex-shrink-0">
          {student.initials}
        </div>
        <div className="min-w-0">
          <p className="text-white font-semibold text-lg leading-tight truncate">
            {student.name}
          </p>
          <p className="text-navy-200 text-sm truncate mt-0.5">
            {student.grade} · {student.track} · {student.examFocus}
          </p>
        </div>
        <div className="ml-auto text-right hidden sm:block flex-shrink-0">
          <p className="text-navy-300 text-xs font-normal">Koç</p>
          <p className="text-white text-sm font-medium">{student.coachName}</p>
        </div>
      </div>
    </div>
  );
};
