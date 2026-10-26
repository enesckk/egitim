import React from 'react';
import { CheckCircle2, Play, Sparkles, Clock } from 'lucide-react';
import { cn } from '@/lib/utils';
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
        'bg-white rounded-2xl border transition-all p-4 shadow-xs select-none',
        isCompleted
          ? 'border-neutral-200/50 bg-neutral-50/50 opacity-80'
          : isInProgress
          ? 'border-blue-300 ring-1 ring-blue-100'
          : 'border-neutral-200/70 hover:border-neutral-300'
      )}
    >
      <div className="flex items-start justify-between gap-3">
        {/* Left Status Toggle */}
        <button
          type="button"
          onClick={() => onToggleStatus(task.id)}
          className="mt-0.5 flex-shrink-0 flex items-center justify-center min-h-[36px] min-w-[36px] rounded-xl hover:bg-neutral-100 transition-colors"
          aria-label={isCompleted ? 'Tamamlandı olarak işaretli' : 'Görevi tamamla'}
        >
          {isCompleted ? (
            <CheckCircle2 className="h-6 w-6 text-emerald-600 stroke-[2.2]" />
          ) : isInProgress ? (
            <span className="w-3.5 h-3.5 rounded-full bg-[#315FE8] ring-4 ring-blue-50" />
          ) : (
            <span className="w-4 h-4 rounded-full border-2 border-neutral-300 hover:border-[#315FE8] transition-colors" />
          )}
        </button>

        {/* Center Details */}
        <div className="flex-1 min-w-0 cursor-pointer" onClick={() => onViewDetails(task)}>
          <div className="flex flex-wrap items-center gap-1.5 mb-1.5">
            <span className="text-[10px] font-bold uppercase tracking-wider text-[#315FE8] bg-blue-50 px-2 py-0.5 rounded-md">
              {task.subject}
            </span>
            {task.isCoachAssigned && (
              <span className="inline-flex items-center text-[10px] font-bold text-purple-700 bg-purple-50 px-2 py-0.5 rounded-md border border-purple-100">
                <Sparkles className="h-2.5 w-2.5 mr-1" />
                Koç Görevi
              </span>
            )}
          </div>

          <h3
            className={cn(
              'text-sm font-bold leading-tight text-[#14233E] transition-colors',
              isCompleted && 'line-through text-neutral-400 font-normal'
            )}
          >
            {task.topic}
          </h3>

          {task.coachNote && (
            <p className="text-xs text-purple-900 bg-purple-50/70 border border-purple-100 rounded-xl px-3 py-2 mt-2 leading-relaxed">
              💡 <strong>Koç Notu:</strong> {task.coachNote}
            </p>
          )}

          <div className="flex items-center gap-3 text-xs text-neutral-400 mt-2.5">
            <span className="flex items-center gap-1 font-medium text-neutral-500">
              <Clock className="h-3.5 w-3.5" />
              {task.durationMinutes} dk planlandı
            </span>
            {task.targetQuestionCount && (
              <span className="flex items-center gap-1 font-mono text-neutral-500">
                🎯 {task.targetQuestionCount} soru
              </span>
            )}
            {isCompleted && task.completedAt && (
              <span className="text-emerald-600 font-semibold">
                ✓ {task.completedAt} tamamlandı
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
              className="flex items-center gap-1.5 text-xs text-white font-semibold bg-[#315FE8] hover:bg-[#254fc9] px-3 py-1.5 rounded-full shadow-xs min-h-[36px]"
            >
              <Play className="h-3 w-3 fill-current" />
              Devam Et
            </button>
          ) : (
            <button
              type="button"
              onClick={() => onViewDetails(task)}
              className="text-xs text-neutral-400 hover:text-[#315FE8] font-medium p-1 transition-colors"
            >
              Detay
            </button>
          )}
        </div>
      </div>
    </div>
  );
};
