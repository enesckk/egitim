import React from 'react';
import { TrendingUp, Award, AlertTriangle, RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';
import { AcademicStatusItem } from '../types';

export interface AcademicStatusCardProps {
  items: AcademicStatusItem[];
}

export const AcademicStatusCard: React.FC<AcademicStatusCardProps> = ({ items }) => {
  const getCategoryConfig = (type: AcademicStatusItem['categoryType']) => {
    switch (type) {
      case 'strengthening':
        return {
          icon: <TrendingUp className="h-4 w-4 text-success" />,
          cardBg: 'bg-success-light border-green-200',
          labelColor: 'text-success-dark',
        };
      case 'developing':
        return {
          icon: <Award className="h-4 w-4 text-primary-600" />,
          cardBg: 'bg-primary-50 border-primary-100',
          labelColor: 'text-primary-700',
        };
      case 'priority':
        return {
          icon: <AlertTriangle className="h-4 w-4 text-warning" />,
          cardBg: 'bg-warning-light border-amber-200',
          labelColor: 'text-warning-dark',
        };
      case 'repetition':
        return {
          icon: <RefreshCw className="h-4 w-4 text-attention" />,
          cardBg: 'bg-attention-light border-purple-200',
          labelColor: 'text-attention-dark',
        };
    }
  };

  return (
    <div className="select-none">
      <h2 className="font-semibold text-neutral-800 text-sm mb-3 px-1">Akademik Durum</h2>
      <div className="space-y-2.5">
        {items.map((item) => {
          const config = getCategoryConfig(item.categoryType);

          return (
            <div
              key={item.category}
              className={cn(
                'rounded-xl border p-3.5 sm:p-4 flex items-start gap-3 transition-colors',
                config.cardBg
              )}
            >
              <div className="mt-0.5 flex-shrink-0">{config.icon}</div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1 flex-wrap">
                  <span className={cn('text-xs font-semibold', config.labelColor)}>
                    {item.category}
                  </span>
                  <div className="flex flex-wrap gap-1">
                    {item.subjects.map((s) => (
                      <span
                        key={s}
                        className="text-xs font-medium text-neutral-700 bg-white/80 px-2 py-0.5 rounded-full border border-neutral-100/50"
                      >
                        {s}
                      </span>
                    ))}
                  </div>
                </div>
                <p className="text-xs text-neutral-600 leading-relaxed">{item.detail}</p>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
