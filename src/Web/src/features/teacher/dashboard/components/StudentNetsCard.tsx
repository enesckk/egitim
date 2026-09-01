import React from 'react';
import { Users, TrendingUp, TrendingDown, ShieldAlert } from 'lucide-react';
import { TeacherStudentNetItem } from '../types';

export interface StudentNetsCardProps {
  students: TeacherStudentNetItem[];
  onSelectStudent: (studentId: string) => void;
}

export const StudentNetsCard: React.FC<StudentNetsCardProps> = ({
  students,
  onSelectStudent,
}) => {
  return (
    <div className="space-y-3 select-none">
      <div className="flex items-center gap-2">
        <Users className="h-4 w-4 text-neutral-500" />
        <h2 className="font-semibold text-neutral-800 text-sm sm:text-base">
          Öğrenci Netlerim
        </h2>
      </div>

      <div className="bg-white rounded-2xl border border-neutral-100 divide-y divide-neutral-50 shadow-soft-sm overflow-hidden">
        {students.map((s) => (
          <button
            key={s.id}
            type="button"
            onClick={() => onSelectStudent(s.studentId)}
            className="w-full text-left flex items-center gap-3 px-4 py-3 hover:bg-neutral-50/70 transition-colors"
          >
            <div className="w-8 h-8 rounded-xl bg-navy-900 text-white text-xs font-semibold flex items-center justify-center flex-shrink-0">
              {s.initials}
            </div>

            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-neutral-900 truncate">
                {s.name}
              </p>
              <p className="text-xs text-neutral-400 font-mono">
                {s.grade}. Sınıf
              </p>
            </div>

            <div className="text-right flex-shrink-0 font-mono text-xs">
              {s.mathNet !== null && (
                <p className="font-semibold text-neutral-800">
                  Mat <strong className="text-primary-700">{s.mathNet}</strong>
                </p>
              )}
              {s.physicsNet !== null && (
                <p className="font-semibold text-neutral-800">
                  Fiz <strong className="text-primary-700">{s.physicsNet}</strong>
                </p>
              )}
            </div>

            <div className="ml-1 flex-shrink-0">
              {s.trend === 'up' && (
                <TrendingUp className="h-3.5 w-3.5 text-success" />
              )}
              {s.trend === 'down' && (
                <TrendingDown className="h-3.5 w-3.5 text-danger" />
              )}
              {s.trend === 'stable' && (
                <span className="w-3 h-0.5 rounded-full bg-neutral-300 inline-block" />
              )}
            </div>
          </button>
        ))}
      </div>

      {/* Scope note — important for privacy and zero-leakage */}
      <div className="bg-surface-alt rounded-xl p-3 border border-neutral-100 flex items-start gap-2 text-[11px] text-neutral-500 leading-relaxed">
        <ShieldAlert className="h-3.5 w-3.5 text-neutral-400 flex-shrink-0 mt-0.5" />
        <span>
          Yalnızca atanmış branşlar ve öğrenciler gösterilmektedir. Koç notları ve özel koçluk gözlemleri bu görünümde yer almaz.
        </span>
      </div>
    </div>
  );
};
