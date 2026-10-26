import React from 'react';
import { cn } from '@/lib/utils';
import { SubjectPlanProgress } from '../types';

export interface PlanSubjectProgressCardProps {
  subjects: SubjectPlanProgress[];
}

export const PlanSubjectProgressCard: React.FC<PlanSubjectProgressCardProps> = ({ subjects }) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-200/70 p-5 shadow-xs select-none">
      <div className="flex items-center justify-between mb-3">
        <h3 className="text-sm font-bold text-[#14233E]">Ders Bazlı Hedefler</h3>
        <span className="text-[11px] text-neutral-400">Planlanan çalışma saatleri</span>
      </div>

      <div className="space-y-3.5">
        {subjects.map((item) => {
          const percentage = item.totalTasks > 0 ? Math.round((item.completedTasks / item.totalTasks) * 100) : 0;
          return (
            <div key={item.subject} className="space-y-1.5">
              <div className="flex items-center justify-between text-xs">
                <span className="font-semibold text-neutral-800">{item.subject}</span>
                <div className="flex items-center gap-2">
                  <span className="text-neutral-500 font-medium">{item.completedHours} sa planlandı</span>
                  <span className="font-bold text-[#14233E]">
                    {item.completedTasks}/{item.totalTasks} (%{percentage})
                  </span>
                </div>
              </div>
              <div className="w-full h-2 bg-neutral-100 rounded-full overflow-hidden">
                <div
                  className={cn('h-full rounded-full transition-all duration-500', item.colorClass)}
                  style={{ width: `${percentage}%` }}
                />
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
