import React from 'react';
import { CheckCircle2, Play, Sparkles, Clock, HelpCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Badge } from '@/components/ui/Badge';
import { PlanTaskItem } from '../types';

export interface PlanTaskCardProps {
  task: PlanTaskItem;
  onToggleStatus: (id: string) => void;
  onViewDetails: (task: PlanTaskItem) => void;
}

export const PlanTaskCard: React.FC<PlanTaskCardProps> = ({
  task,
  onToggleStatus,
  onViewDetails,
}) => {
  const isCompleted = task.status === 'completed';
  const isInProgress = task.status === 'in_progress';

  return (
    <div
      className={cn(
        'bg-white rounded-2xl border transition-all p-3.5 sm:p-4 select-none',
        isCompleted
          ? 'border-neutral-100 bg-neutral-50/40 opacity-75'
          : isInProgress
          ? 'border-primary-200 ring-1 ring-primary-100'
          : 'border-neutral-100 hover:border-neutral-200'
      )}
    >
      <div className="flex items-start justify-between gap-3">
        {/* Left Status Toggle */}
        <button
          type="button"
          onClick={() => onToggleStatus(task.id)}
          className="mt-0.5 flex-shrink-0 flex items-center justify-center min-h-[32px] min-w-[32px] rounded-lg hover:bg-neutral-100 transition-colors"
          aria-label={isCompleted ? 'Tamamlandı olarak işaretli' : 'Görevi tamamla'}
        >
          {isCompleted ? (
            <CheckCircle2 className="h-5 w-5 text-success stroke-[2.2]" />
          ) : isInProgress ? (
            <span className="w-3 h-3 rounded-full bg-primary-500 ring-4 ring-primary-50" />
          ) : (
            <span className="w-3 h-3 rounded-full border-2 border-neutral-300" />
          )}
        </button>

        {/* Center Details */}
        <div className="flex-1 min-w-0" onClick={() => onViewDetails(task)}>
          <div className="flex flex-wrap items-center gap-1.5 mb-1">
            <span className="text-[11px] font-semibold uppercase tracking-wider text-primary-700 bg-primary-50 px-2 py-0.5 rounded-md">
              {task.subject}
            </span>
            {task.isCoachAssigned && (
              <Badge variant="attention" size="sm">
                <Sparkles className="h-2.5 w-2.5 mr-1" />
                Koç Ödevi
              </Badge>
            )}
          </div>

          <h3
            className={cn(
              'text-sm font-semibold leading-tight text-neutral-900 cursor-pointer hover:text-primary-600 transition-colors',
              isCompleted && 'line-through text-neutral-500'
            )}
          >
            {task.topic}
          </h3>

          {task.coachNote && (
            <p className="text-xs text-attention-dark bg-attention-light border border-purple-100 rounded-lg px-2.5 py-1.5 mt-2 leading-relaxed">
              💡 <strong>Koç Notu:</strong> {task.coachNote}
            </p>
          )}

          <div className="flex items-center gap-3 text-xs text-neutral-400 mt-2">
            <span className="flex items-center gap-1">
              <Clock className="h-3 w-3" />
              {task.durationMinutes} dk
            </span>
            {task.targetQuestionCount && (
              <span className="flex items-center gap-1 font-mono">
                🎯 {task.targetQuestionCount} soru
              </span>
            )}
            {isCompleted && task.completedAt && (
              <span className="text-success font-medium">
                ✓ {task.completedAt}&apos;de bitti
              </span>
            )}
          </div>
        </div>

        {/* Right Action */}
        <div className="flex flex-col items-end gap-1.5 flex-shrink-0">
          {isInProgress ? (
            <button
              type="button"
              onClick={() => onToggleStatus(task.id)}
              className="flex items-center gap-1 text-xs text-primary-600 font-semibold bg-primary-50 px-2.5 py-1.5 rounded-lg hover:bg-primary-100 min-h-[36px]"
            >
              <Play className="h-3 w-3 fill-current" />
              Devam Et
            </button>
          ) : (
            <button
              type="button"
              onClick={() => onViewDetails(task)}
              className="p-1.5 text-neutral-300 hover:text-neutral-600 rounded-lg transition-colors"
              aria-label="Detayları Gör"
            >
              <HelpCircle className="h-4 w-4" />
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
