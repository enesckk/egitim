import React from 'react';
import { CheckCircle2, Play } from 'lucide-react';
import { cn } from '@/lib/utils';
import { TodayPlanItem } from '../types';

export interface TodayPlanTabProps {
  plans: TodayPlanItem[];
  onTogglePlanStatus?: (id: number) => void;
  onContinuePlan?: (id: number) => void;
}

export const TodayPlanTab: React.FC<TodayPlanTabProps> = ({
  plans,
  onTogglePlanStatus,
  onContinuePlan,
}) => {
  const completedCount = plans.filter((p) => p.status === 'completed').length;
  const progressPercentage = plans.length > 0 ? (completedCount / plans.length) * 100 : 0;

  return (
    <div>
      {/* Plan Items List */}
      <div className="divide-y divide-neutral-50">
        {plans.map((item, index) => {
          const isCompleted = item.status === 'completed';
          const isActive = item.status === 'active';
          const isUpcoming = item.status === 'upcoming';

          return (
            <div
              key={item.id}
              onClick={() => onTogglePlanStatus?.(item.id)}
              className={cn(
                'flex items-center justify-between gap-2.5 sm:gap-4 px-4 sm:px-5 py-2.5 sm:py-3.5 transition-colors select-none',
                !isCompleted && 'hover:bg-neutral-50/80 cursor-pointer',
                isCompleted && 'opacity-70'
              )}
            >
              <div className="flex items-center gap-2.5 sm:gap-3 min-w-0 flex-1">
                {/* Status Indicator */}
                <div className="flex-shrink-0 w-5 h-5 flex items-center justify-center">
                  {isCompleted ? (
                    <CheckCircle2 className="h-[18px] w-[18px] text-success stroke-[2.2]" />
                  ) : isActive ? (
                    <span className="w-2 h-2 rounded-full bg-primary-500 ring-4 ring-primary-50" />
                  ) : (
                    <span className="w-2 h-2 rounded-full bg-neutral-300" />
                  )}
                </div>

                {/* Task Details */}
                <div className="min-w-0 flex-1 pr-1">
                  <p
                    className={cn(
                      'text-xs sm:text-sm font-medium leading-tight',
                      isCompleted ? 'text-neutral-500 line-through' : 'text-neutral-900'
                    )}
                  >
                    {item.topic}
                  </p>
                  <p className="text-[11px] sm:text-xs text-neutral-400 mt-0.5 truncate">
                    {item.subject} • {item.durationMinutes} dk
                    {isCompleted && item.completedAt && (
                      <span className="ml-1 text-success font-medium">
                        • {item.completedAt}&apos;de tamamlandı
                      </span>
                    )}
                  </p>
                </div>
              </div>

              {/* Action / Index on Right */}
              {isActive && (
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onContinuePlan?.(item.id);
                  }}
                  className="flex-shrink-0 flex items-center gap-1 text-[11px] font-medium text-primary-600 bg-primary-50 px-2.5 py-1 rounded-lg hover:bg-primary-100 active:bg-primary-200 transition-colors focus:outline-none focus:ring-2 focus:ring-primary-500 min-h-[30px]"
                >
                  <Play className="h-2.5 w-2.5 fill-current" />
                  Devam Et
                </button>
              )}

              {isUpcoming && (
                <span className="flex-shrink-0 text-xs font-mono text-neutral-300 pr-1">
                  #{index + 1}
                </span>
              )}
            </div>
          );
        })}
      </div>

      {/* Daily Progress Bar Section */}
      <div className="px-4 sm:px-5 py-2.5 sm:py-3 bg-neutral-50 border-t border-neutral-100">
        <div className="flex items-center justify-between mb-1.5">
          <span className="text-xs font-medium text-neutral-600">Günlük ilerleme</span>
          <span className="text-xs font-mono font-semibold text-neutral-700">
            {completedCount}/{plans.length} tamamlandı
          </span>
        </div>
        <div className="h-1.5 bg-neutral-200 rounded-full overflow-hidden">
          <div
            className="h-full bg-primary-500 rounded-full transition-all duration-500"
            style={{ width: `${progressPercentage}%` }}
          />
        </div>
      </div>
    </div>
  );
};
