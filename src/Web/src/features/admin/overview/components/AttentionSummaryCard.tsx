import React from 'react';
import { AlertTriangle } from 'lucide-react';
import { AttentionStatItem } from '../types';

export interface AttentionSummaryCardProps {
  stats: AttentionStatItem[];
}

export const AttentionSummaryCard: React.FC<AttentionSummaryCardProps> = ({ stats }) => {
  return (
    <div className="bg-danger-light border border-red-200 rounded-2xl p-4 sm:p-5 select-none shadow-soft-sm">
      <div className="flex items-center gap-2 mb-3">
        <AlertTriangle className="h-4 w-4 text-danger flex-shrink-0" />
        <h3 className="font-semibold text-danger text-sm">Dikkat Gerektiren</h3>
      </div>
      <div className="space-y-2.5">
        {stats.map((item) => (
          <div key={item.label} className="bg-white/70 rounded-xl p-3 border border-red-100">
            <div className="flex items-baseline justify-between">
              <span className="text-xs font-medium text-neutral-700">{item.label}</span>
              <span className="font-mono text-lg font-bold text-danger">{item.value}</span>
            </div>
            <p className="text-[11px] text-neutral-400 mt-0.5">{item.sub}</p>
          </div>
        ))}
      </div>
    </div>
  );
};
