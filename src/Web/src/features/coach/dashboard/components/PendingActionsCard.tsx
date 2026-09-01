import React from 'react';
import { MessageSquare, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { PendingActionItem } from '../types';

export interface PendingActionsCardProps {
  actions: PendingActionItem[];
  onReviewAction: (action: PendingActionItem) => void;
}

export const PendingActionsCard: React.FC<PendingActionsCardProps> = ({
  actions,
  onReviewAction,
}) => {
  const urgentCount = actions.filter((a) => a.urgent).length;

  return (
    <div className="space-y-3 select-none">
      <div className="flex items-center gap-2">
        <MessageSquare className="h-4 w-4 text-neutral-500" />
        <h2 className="font-semibold text-neutral-800 text-sm">Bekleyen İşlemler</h2>
        {urgentCount > 0 && (
          <span className="inline-flex items-center justify-center w-4 h-4 rounded-full bg-danger text-white text-[10px] font-bold">
            {urgentCount}
          </span>
        )}
      </div>

      <div className="bg-white rounded-2xl border border-neutral-100 divide-y divide-neutral-50 shadow-soft-sm overflow-hidden">
        {actions.map((action) => (
          <div key={action.id} className="flex items-center gap-3 px-4 py-3 hover:bg-neutral-50/50 transition-colors">
            <div
              className={cn(
                'w-2 h-2 rounded-full flex-shrink-0',
                action.urgent ? 'bg-danger' : 'bg-neutral-300'
              )}
            />
            <div className="flex-1 min-w-0">
              <p className="text-sm font-semibold text-neutral-800 truncate">{action.type}</p>
              <p className="text-xs text-neutral-400 truncate">{action.studentName} • {action.time}</p>
            </div>
            <button
              type="button"
              onClick={() => onReviewAction(action)}
              className="text-xs text-primary-600 font-semibold hover:text-primary-700 whitespace-nowrap flex items-center gap-0.5"
            >
              İncele
              <ChevronRight className="h-3 w-3" />
            </button>
          </div>
        ))}

        {actions.length === 0 && (
          <div className="px-4 py-6 text-center text-xs text-neutral-400">
            Bekleyen herhangi bir onay veya talep işlemi bulunmuyor.
          </div>
        )}
      </div>
    </div>
  );
};
