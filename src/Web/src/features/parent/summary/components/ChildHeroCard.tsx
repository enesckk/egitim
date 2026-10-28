import React from 'react';
import { UserCheck, GraduationCap } from 'lucide-react';
import { LinkedStudentSummary } from '../types';

export interface ChildHeroCardProps {
  student: LinkedStudentSummary;
}

export const ChildHeroCard: React.FC<ChildHeroCardProps> = ({ student }) => {
  return (
    <div className="bg-[#14233E] rounded-3xl p-5 sm:p-7 text-white shadow-soft-sm relative overflow-hidden border border-navy-800 select-none">
      <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
        {/* Child Identity */}
        <div className="flex items-center gap-4 min-w-0">
          <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl bg-gradient-to-br from-blue-600 to-[#14233E] border border-blue-400/30 text-white font-serif font-bold text-lg sm:text-xl flex items-center justify-center shrink-0 shadow-soft-xs">
            {student.initials}
          </div>

          <div className="min-w-0 flex-1">
            <div className="flex items-center gap-2 mb-1 flex-wrap">
              <span className="text-xs px-2.5 py-0.5 rounded-full bg-navy-800/80 text-blue-300 border border-navy-700 font-medium">
                {student.grade}
              </span>
              <span className="text-navy-500">•</span>
              <span className="text-xs text-navy-300 font-medium">
                {student.track}
              </span>
            </div>

            <h1 className="font-serif text-2xl sm:text-3xl font-bold tracking-tight text-white leading-tight truncate">
              {student.name}
            </h1>

            <div className="flex items-center gap-3 text-xs text-navy-200 mt-1 flex-wrap">
              <span className="flex items-center gap-1">
                <GraduationCap className="h-3.5 w-3.5 text-blue-300" />
                <span>Hedef: {student.examFocus}</span>
              </span>
              {student.studentNumber && (
                <>
                  <span className="text-navy-600">•</span>
                  <span className="font-mono text-navy-300">No: {student.studentNumber}</span>
                </>
              )}
            </div>
          </div>
        </div>

        {/* Assigned Coach Banner */}
        <div className="bg-navy-800/80 rounded-2xl p-3.5 sm:px-4.5 border border-navy-700/80 flex items-center gap-3 shrink-0 self-start md:self-auto">
          <div className="w-9 h-9 rounded-xl bg-primary-600/20 border border-primary-500/30 text-blue-300 flex items-center justify-center shrink-0">
            <UserCheck className="h-4.5 w-4.5 text-blue-400" />
          </div>
          <div>
            <p className="text-[11px] uppercase tracking-wider text-navy-300 font-semibold leading-none">
              {student.coachTitle}
            </p>
            <p className="text-sm font-bold text-white mt-1 leading-tight">
              {student.coachName}
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
