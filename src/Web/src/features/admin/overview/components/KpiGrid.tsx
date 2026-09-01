import React from 'react';
import { Users, CheckCircle2, Clock } from 'lucide-react';
import { cn } from '@/lib/utils';
import { InstitutionKpiItem } from '../types';

export interface KpiGridProps {
  kpis: InstitutionKpiItem[];
}

export const KpiGrid: React.FC<KpiGridProps> = ({ kpis }) => {
  const getIcon = (type: InstitutionKpiItem['iconType']) => {
    switch (type) {
      case 'users':
        return <Users className="h-4 w-4 text-primary-600" />;
      case 'coaches':
        return <Users className="h-4 w-4 text-navy-700" />;
      case 'adherence':
        return <CheckCircle2 className="h-4 w-4 text-success" />;
      case 'clock':
        return <Clock className="h-4 w-4 text-warning" />;
    }
  };

  return (
    <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4 select-none">
      {kpis.map((kpi) => (
        <div
          key={kpi.label}
          className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm hover:border-neutral-200 transition-colors"
        >
          <div className={cn('w-8 h-8 rounded-lg flex items-center justify-center mb-3', kpi.color)}>
            {getIcon(kpi.iconType)}
          </div>
          <p className="font-mono text-2xl font-bold text-neutral-900 leading-tight">
            {kpi.value}
          </p>
          <p className="text-xs font-semibold text-neutral-700 mt-1">{kpi.label}</p>
          <p className="text-xs text-neutral-400 mt-0.5">{kpi.sub}</p>
        </div>
      ))}
    </div>
  );
};
