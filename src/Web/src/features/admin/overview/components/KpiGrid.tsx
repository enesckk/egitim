import React from 'react';
import { Users, UserCheck, CheckCircle2, Clock } from 'lucide-react';
import { InstitutionKpiItem } from '../types';

export interface KpiGridProps {
  kpis: InstitutionKpiItem[];
}

export const KpiGrid: React.FC<KpiGridProps> = ({ kpis }) => {
  const getIcon = (type: InstitutionKpiItem['iconType']) => {
    switch (type) {
      case 'users':
        return <Users className="h-5 w-5 text-[#315FE8]" />;
      case 'coaches':
        return <UserCheck className="h-5 w-5 text-[#14233E]" />;
      case 'adherence':
        return <CheckCircle2 className="h-5 w-5 text-emerald-600" />;
      case 'clock':
        return <Clock className="h-5 w-5 text-amber-600" />;
    }
  };

  const getBadgeBg = (type: InstitutionKpiItem['iconType']) => {
    switch (type) {
      case 'users':
        return 'bg-blue-50 border-blue-100 text-[#315FE8]';
      case 'coaches':
        return 'bg-navy-50 border-navy-100 text-[#14233E]';
      case 'adherence':
        return 'bg-emerald-50 border-emerald-100 text-emerald-700';
      case 'clock':
        return 'bg-amber-50 border-amber-100 text-amber-700';
    }
  };

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3.5 sm:gap-4 select-none">
      {kpis.map((kpi) => (
        <div
          key={kpi.label}
          className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs hover:border-neutral-300 transition-all flex flex-col justify-between"
        >
          <div className="flex items-center justify-between gap-2 mb-3">
            <span className="text-xs font-semibold text-neutral-600 uppercase tracking-wider">
              {kpi.label}
            </span>
            <div className={`w-9 h-9 rounded-xl flex items-center justify-center border shadow-soft-xs shrink-0 ${getBadgeBg(kpi.iconType)}`}>
              {getIcon(kpi.iconType)}
            </div>
          </div>

          <div>
            <p className="font-mono text-2xl sm:text-3xl font-bold text-navy-950 tracking-tight leading-none">
              {kpi.value}
            </p>
            <p className="text-xs text-neutral-400 mt-2 font-medium">
              {kpi.sub}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
};
