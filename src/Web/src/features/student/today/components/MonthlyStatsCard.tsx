import React from 'react';
import { Award, Clock, TrendingUp } from 'lucide-react';
import { MonthlyStatItem } from '../types';

export interface MonthlyStatsCardProps {
  stats: MonthlyStatItem[];
}

export const MonthlyStatsCard: React.FC<MonthlyStatsCardProps> = ({ stats }) => {
  const getStatIcon = (type: MonthlyStatItem['type']) => {
    switch (type) {
      case 'award':
        return <Award className="h-3.5 w-3.5 text-warning" />;
      case 'clock':
        return <Clock className="h-3.5 w-3.5 text-primary-500" />;
      case 'trend':
      case 'exam':
      default:
        return <TrendingUp className="h-3.5 w-3.5 text-success" />;
    }
  };

  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-4">
      <h3 className="text-sm font-semibold text-neutral-700 mb-3">Bu Ay</h3>
      <div className="grid grid-cols-2 gap-3">
        {stats.map((stat) => (
          <div key={stat.label} className="bg-neutral-50 rounded-xl p-3">
            <div className="flex items-center gap-1.5 mb-1">
              {getStatIcon(stat.type)}
            </div>
            <p className="text-lg font-mono font-semibold text-neutral-900 leading-none">
              {stat.value}
            </p>
            <p className="text-xs text-neutral-400 mt-1">{stat.label}</p>
          </div>
        ))}
      </div>
    </div>
  );
};
