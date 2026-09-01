import React from 'react';
import { cn } from '@/lib/utils';
import { SubjectPlanProgress } from '../types';

export interface PlanSubjectProgressCardProps {
  subjects: SubjectPlanProgress[];
}

export const PlanSubjectProgressCard: React.FC<PlanSubjectProgressCardProps> = ({ subjects }) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-4 select-none">
      <h3 className="text-sm font-semibold text-neutral-800 mb-3">Ders Bazlı Hedefler</h3>
      <div className="space-y-3">
        {subjects.map((item) => {
          const percentage = Math.round((item.completedTasks / item.totalTasks) * 100);
          return (
            <div key={item.subject} className="space-y-1">
              <div className="flex items-center justify-between text-xs">
                <span className="font-medium text-neutral-700">{item.subject}</span>
                <div className="flex items-center gap-2">
                  <span className="font-mono text-neutral-400">{item.completedHours} sa</span>
                  <span className="font-mono font-semibold text-neutral-800">
                    {item.completedTasks}/{item.totalTasks} ({percentage}%)
                  </span>
                </div>
              </div>
              <div className="w-full h-1.5 bg-neutral-100 rounded-full overflow-hidden">
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
