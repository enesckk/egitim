import React from 'react';
import { TrendingUp } from 'lucide-react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { TytTrendPoint } from '../types';

export interface AcademicTrendChartProps {
  data: TytTrendPoint[];
  timeframeLabel: string;
}

const LINE_COLORS = {
  turkce: '#2451B7',
  matematik: '#059669',
  sosyal: '#D97706',
  fen: '#7C3AED',
};

export const AcademicTrendChart: React.FC<AcademicTrendChartProps> = ({
  data,
  timeframeLabel,
}) => {
  const lastData = data[data.length - 1] || { turkce: 0, matematik: 0 };
  const firstData = data[0] || { turkce: 0, matematik: 0 };

  const turkceChange = lastData.turkce - firstData.turkce;
  const matChange = lastData.matematik - firstData.matematik;

  return (
    <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm select-none">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-5">
        <div>
          <h2 className="font-semibold text-neutral-800 text-sm">TYT Net Ortalaması</h2>
          <p className="text-xs text-neutral-400 mt-0.5">{timeframeLabel} — Kurum geneli</p>
        </div>
        <div className="flex items-center gap-3 text-xs text-neutral-500 flex-wrap">
          <span className="flex items-center gap-1">
            <span className="inline-block w-3 h-0.5 rounded-full bg-[#2451B7]" />
            Türkçe
          </span>
          <span className="flex items-center gap-1">
            <span className="inline-block w-3 h-0.5 rounded-full bg-[#059669]" />
            Matematik
          </span>
          <span className="flex items-center gap-1">
            <span className="inline-block w-3 h-0.5 rounded-full bg-[#D97706]" />
            Sosyal
          </span>
          <span className="flex items-center gap-1">
            <span className="inline-block w-3 h-0.5 rounded-full bg-[#7C3AED]" />
            Fen
          </span>
        </div>
      </div>

      <div className="w-full h-[220px]">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={data} margin={{ top: 5, right: 5, bottom: 0, left: -10 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#F1F5F9" />
            <XAxis
              dataKey="ay"
              tick={{ fontSize: 11, fill: '#94A3B8' }}
              axisLine={false}
              tickLine={false}
            />
            <YAxis
              tick={{ fontSize: 11, fill: '#94A3B8' }}
              axisLine={false}
              tickLine={false}
              domain={[10, 35]}
            />
            <Tooltip
              contentStyle={{
                background: '#0F1B2D',
                border: 'none',
                borderRadius: 10,
                fontSize: 12,
                color: '#F8FAFC',
              }}
              labelStyle={{ color: '#A3BCE0', marginBottom: 4 }}
              // eslint-disable-next-line @typescript-eslint/no-explicit-any
              formatter={(value: any) => [`${Number(value || 0).toFixed(1)} net`, '']}
            />
            <Line
              type="monotone"
              dataKey="turkce"
              name="Türkçe"
              stroke={LINE_COLORS.turkce}
              strokeWidth={2}
              dot={{ r: 3, fill: LINE_COLORS.turkce }}
              activeDot={{ r: 5 }}
            />
            <Line
              type="monotone"
              dataKey="matematik"
              name="Matematik"
              stroke={LINE_COLORS.matematik}
              strokeWidth={2}
              dot={{ r: 3, fill: LINE_COLORS.matematik }}
              activeDot={{ r: 5 }}
            />
            <Line
              type="monotone"
              dataKey="sosyal"
              name="Sosyal"
              stroke={LINE_COLORS.sosyal}
              strokeWidth={2}
              dot={{ r: 3, fill: LINE_COLORS.sosyal }}
              activeDot={{ r: 5 }}
            />
            <Line
              type="monotone"
              dataKey="fen"
              name="Fen"
              stroke={LINE_COLORS.fen}
              strokeWidth={2}
              dot={{ r: 3, fill: LINE_COLORS.fen }}
              activeDot={{ r: 5 }}
            />
          </LineChart>
        </ResponsiveContainer>
      </div>

      {/* Trend summary */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-4 pt-4 border-t border-neutral-100">
        <div className="flex items-center gap-2">
          <TrendingUp className="h-4 w-4 text-success flex-shrink-0" />
          <span className="text-xs text-neutral-600">
            Türkçe: Son periyotta <strong>+{turkceChange.toFixed(1)} net</strong> artış
          </span>
        </div>
        <div className="flex items-center gap-2">
          <TrendingUp className="h-4 w-4 text-success flex-shrink-0" />
          <span className="text-xs text-neutral-600">
            Matematik: Son periyotta <strong>+{matChange.toFixed(1)} net</strong> artış
          </span>
        </div>
      </div>
    </div>
  );
};
