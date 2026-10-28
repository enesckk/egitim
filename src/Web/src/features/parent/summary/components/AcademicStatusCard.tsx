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
          icon: <TrendingUp className="h-4 w-4 text-emerald-600" />,
          cardBg: 'bg-emerald-50/60 border-emerald-200/80',
          labelColor: 'text-emerald-800',
        };
      case 'developing':
        return {
          icon: <Award className="h-4 w-4 text-blue-600" />,
          cardBg: 'bg-blue-50/60 border-blue-200/80',
          labelColor: 'text-blue-800',
        };
      case 'priority':
        return {
          icon: <AlertTriangle className="h-4 w-4 text-amber-600" />,
          cardBg: 'bg-amber-50/60 border-amber-200/80',
          labelColor: 'text-amber-800',
        };
      case 'repetition':
        return {
          icon: <RefreshCw className="h-4 w-4 text-purple-600" />,
          cardBg: 'bg-purple-50/60 border-purple-200/80',
          labelColor: 'text-purple-800',
        };
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs select-none space-y-3.5">
      <div className="flex items-center justify-between">
        <h2 className="font-bold text-neutral-900 text-sm sm:text-base">
          Akademik Durum ve Ders Odakları
        </h2>
        <span className="text-xs text-neutral-400">Son deneme analizleri bazlı</span>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        {items.map((item) => {
          const config = getCategoryConfig(item.categoryType);

          return (
            <div
              key={item.category}
              className={cn(
                'rounded-xl border p-3.5 flex items-start gap-3 transition-colors',
                config.cardBg
              )}
            >
              <div className="mt-0.5 shrink-0">{config.icon}</div>
              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1 flex-wrap">
                  <span className={cn('text-xs font-bold', config.labelColor)}>
                    {item.category}
                  </span>
                  <div className="flex flex-wrap gap-1">
                    {item.subjects.map((s) => (
                      <span
                        key={s}
                        className="text-xs font-medium text-neutral-800 bg-white px-2 py-0.5 rounded-full border border-neutral-200 shadow-soft-xs"
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
