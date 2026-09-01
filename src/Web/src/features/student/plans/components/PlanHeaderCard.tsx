import React from 'react';
import { Calendar, UserCheck, CheckCircle2 } from 'lucide-react';
import { Progress } from '@/components/ui/Progress';
import { StudentPlanViewModel } from '../types';

export interface PlanHeaderCardProps {
  plan: StudentPlanViewModel;
}

export const PlanHeaderCard: React.FC<PlanHeaderCardProps> = ({ plan }) => {
  return (
    <div className="bg-navy-900 rounded-2xl p-4 sm:p-5 text-white relative overflow-hidden select-none">
      <div
        className="absolute top-0 right-0 w-36 h-36 rounded-full bg-navy-800 -mr-10 -mt-10 opacity-50 pointer-events-none"
        aria-hidden="true"
      />

      <div className="relative z-10 space-y-3">
        {/* Week and Academic Goal Badges */}
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-1.5 bg-navy-800 text-primary-300 px-2.5 py-1 rounded-lg text-xs font-medium border border-navy-700">
            <Calendar className="h-3.5 w-3.5" />
            <span>{plan.currentWeek} ({plan.dateRange})</span>
          </div>

          <div className="flex items-center gap-1.5 text-navy-300 text-xs">
            <UserCheck className="h-3.5 w-3.5 text-primary-400" />
            <span>Koç: <strong className="text-white font-medium">{plan.coachName}</strong></span>
          </div>
        </div>

        {/* Title */}
        <div>
          <h2 className="text-white font-serif text-lg sm:text-xl leading-tight">
            {plan.title}
          </h2>
          <p className="text-navy-300 text-xs mt-0.5">
            Hedef: {plan.academicGoal}
          </p>
        </div>

        {/* Progress Bar & Stats */}
        <div className="pt-1">
          <div className="flex items-center justify-between text-xs mb-1.5">
            <span className="text-navy-300">Haftalık Tamamlanma</span>
            <div className="flex items-center gap-2">
              <span className="font-mono text-white font-semibold">
                {plan.completedTasks}/{plan.totalTasks} Görev
              </span>
              <span className="font-mono text-success font-semibold flex items-center gap-0.5">
                <CheckCircle2 className="h-3 w-3" />
                %{plan.completionRate}
              </span>
            </div>
          </div>
          <Progress value={plan.completionRate} variant="primary" size="md" />
        </div>
      </div>
    </div>
  );
};
