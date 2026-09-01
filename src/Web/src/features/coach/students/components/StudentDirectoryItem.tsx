import React from 'react';
import { ChevronRight, CheckCircle2, AlertTriangle, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { AssignedStudentDetail, StudentStatus } from '../types';

export interface StudentDirectoryItemProps {
  student: AssignedStudentDetail;
  onClick: (student: AssignedStudentDetail) => void;
}

const STATUS_MAP: Record<
  StudentStatus,
  { label: string; icon: React.ReactNode; badgeClass: string }
> = {
  takipte: {
    label: 'Takipte',
    icon: <CheckCircle2 className="h-3.5 w-3.5 text-success" />,
    badgeClass: 'bg-success-light text-success-dark border-green-200',
  },
  dikkat: {
    label: 'Dikkat',
    icon: <AlertTriangle className="h-3.5 w-3.5 text-warning" />,
    badgeClass: 'bg-warning-light text-warning-dark border-amber-200',
  },
  kritik: {
    label: 'Kritik',
    icon: <XCircle className="h-3.5 w-3.5 text-danger" />,
    badgeClass: 'bg-danger-light text-danger-dark border-red-200',
  },
};

export const StudentDirectoryItem: React.FC<StudentDirectoryItemProps> = ({
  student,
  onClick,
}) => {
  const statusInfo = STATUS_MAP[student.status];

  return (
    <div
      onClick={() => onClick(student)}
      className="bg-white rounded-2xl border border-neutral-100 hover:border-neutral-200 hover:shadow-soft-sm transition-all p-3.5 sm:p-4 cursor-pointer select-none"
    >
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        {/* Left: Avatar & Identity */}
        <div className="flex items-center gap-3 min-w-0">
          <div className="w-10 h-10 rounded-2xl bg-navy-900 text-white font-semibold text-sm flex items-center justify-center flex-shrink-0">
            {student.initials}
          </div>

          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <h3 className="text-sm sm:text-base font-semibold text-neutral-900 truncate">
                {student.name}
              </h3>
              <span
                className={cn(
                  'inline-flex items-center gap-1 text-[10px] font-semibold uppercase px-2 py-0.5 rounded-full border',
                  statusInfo.badgeClass
                )}
              >
                {statusInfo.icon}
                {statusInfo.label}
              </span>
            </div>

            <p className="text-xs text-neutral-400 truncate mt-0.5">
              {student.grade} • {student.field} ({student.exam})
            </p>
          </div>
        </div>

        {/* Right: Metrics & CTA */}
        <div className="flex items-center justify-between sm:justify-end gap-4 border-t sm:border-t-0 pt-2 sm:pt-0 border-neutral-50">
          <div className="text-left sm:text-right">
            <span className="text-[10px] text-neutral-400 block uppercase">Plan Uyumu</span>
            <span
              className={cn(
                'font-mono text-sm sm:text-base font-bold',
                student.planAdherence >= 80
                  ? 'text-success'
                  : student.planAdherence >= 50
                  ? 'text-warning'
                  : 'text-danger'
              )}
            >
              %{student.planAdherence}
            </span>
          </div>

          <div className="text-left sm:text-right hidden sm:block">
            <span className="text-[10px] text-neutral-400 block uppercase">Son Etkinlik</span>
            <span className="text-xs text-neutral-600 font-medium">
              {student.lastActivity}
            </span>
          </div>

          <div className="w-8 h-8 rounded-full bg-neutral-50 flex items-center justify-center text-neutral-400 hover:text-primary-600 transition-colors">
            <ChevronRight className="h-4 w-4" />
          </div>
        </div>
      </div>
    </div>
  );
};
