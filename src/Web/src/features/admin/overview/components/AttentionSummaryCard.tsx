import React from 'react';
import { AlertCircle, AlertTriangle } from 'lucide-react';
import { AttentionStatItem } from '../types';

export interface AttentionSummaryCardProps {
  stats: AttentionStatItem[];
}

export const AttentionSummaryCard: React.FC<AttentionSummaryCardProps> = ({ stats }) => {
  return (
    <div className="bg-white border border-rose-200/90 rounded-2xl p-4 sm:p-5 select-none shadow-soft-xs flex flex-col justify-between h-full">
      <div>
        <div className="flex items-center gap-2 mb-3.5 pb-2.5 border-b border-rose-100">
          <div className="w-8 h-8 rounded-lg bg-rose-50 border border-rose-200 flex items-center justify-center shrink-0">
            <AlertTriangle className="h-4 w-4 text-rose-600" />
          </div>
          <div>
            <h3 className="font-bold text-navy-950 text-sm">Dikkat Gerektiren Alanlar</h3>
            <p className="text-[11px] text-neutral-500">Müdahale ve destek bekleyen durumlar</p>
          </div>
        </div>

        <div className="space-y-2.5">
          {stats.map((item) => {
            const hasCount = parseInt(item.value, 10) > 0;
            return (
              <div
                key={item.label}
                className={`rounded-xl p-3 border transition-colors ${
                  hasCount
                    ? 'bg-rose-50/60 border-rose-200/80 text-rose-950'
                    : 'bg-neutral-50/70 border-neutral-200/70 text-neutral-700'
                }`}
              >
                <div className="flex items-baseline justify-between gap-2">
                  <span className="text-xs font-semibold">{item.label}</span>
                  <span className={`font-mono text-lg font-bold ${hasCount ? 'text-rose-600' : 'text-neutral-500'}`}>
                    {item.value}
                  </span>
                </div>
                <p className="text-[11px] text-neutral-500 mt-0.5">{item.sub}</p>
              </div>
            );
          })}
        </div>
      </div>

      <div className="mt-3.5 pt-2.5 border-t border-neutral-100 flex items-center gap-1.5 text-[11px] text-neutral-400">
        <AlertCircle className="h-3.5 w-3.5 text-neutral-400 shrink-0" />
        <span>Veriler koç çalışma planları ve deneme yoklamalarından türetilir.</span>
      </div>
    </div>
  );
};
