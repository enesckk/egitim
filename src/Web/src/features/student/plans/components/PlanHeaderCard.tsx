import React from 'react';
import { Calendar, UserCheck, CheckCircle2 } from 'lucide-react';
import { Progress } from '@/components/ui/Progress';
import { StudentPlanViewModel } from '../types';

export interface PlanHeaderCardProps {
  plan: StudentPlanViewModel;
}

export const PlanHeaderCard: React.FC<PlanHeaderCardProps> = ({ plan }) => {
  return (
    <div className="bg-[#14233E] rounded-2xl p-5 sm:p-6 text-white relative overflow-hidden shadow-xs select-none">
      {/* Subtle background glow */}
      <div
        className="absolute -top-12 -right-12 w-36 h-36 bg-[#315FE8]/25 rounded-full blur-2xl pointer-events-none"
        aria-hidden="true"
      />

      <div className="relative z-10 space-y-3.5">
        {/* Week and Academic Goal Badges */}
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="flex items-center gap-1.5 bg-white/10 text-blue-200 px-3 py-1 rounded-full text-xs font-semibold border border-white/10">
            <Calendar className="h-3.5 w-3.5" />
            <span data-testid="plan-date-range">{plan.currentWeek} ({plan.dateRange})</span>
          </div>

          <div className="flex items-center gap-1.5 text-blue-200/90 text-xs font-medium">
            <UserCheck className="h-3.5 w-3.5 text-blue-300" />
            <span>Koç: <strong className="text-white font-semibold">{plan.coachName}</strong></span>
          </div>
        </div>

        {/* Title */}
        <div>
          <h2 className="text-white text-lg sm:text-xl font-bold tracking-tight leading-tight">
            {plan.title}
          </h2>
          <p className="text-blue-100/80 text-xs mt-1">
            Hedef: {plan.academicGoal}
          </p>
        </div>

        {/* Progress Bar & Stats */}
        <div className="pt-1.5">
          <div className="flex items-center justify-between text-xs mb-2">
            <span className="text-blue-200 font-medium">Plan İlerlemesi</span>
            <div className="flex items-center gap-2">
              <span className="text-white font-bold">
                {plan.completedTasks}/{plan.totalTasks} Görev
              </span>
              <span className="text-emerald-400 font-bold flex items-center gap-1">
                <CheckCircle2 className="h-3.5 w-3.5" />
                %{plan.completionRate}
              </span>
            </div>
          </div>
          <Progress value={plan.completionRate} variant="primary" size="md" className="bg-white/15 h-2.5 rounded-full" />
        </div>
      </div>
    </div>
  );
};
