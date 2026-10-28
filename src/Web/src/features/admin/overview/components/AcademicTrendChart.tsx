import React from 'react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import { TrendingUp, BarChart2 } from 'lucide-react';
import { TytTrendPoint } from '../types';

export interface AcademicTrendChartProps {
  data: TytTrendPoint[];
}

const LINE_COLORS = {
  turkce: '#315FE8',    // Brand Blue
  matematik: '#059669', // Emerald
  sosyal: '#D97706',    // Amber
  fen: '#7C3AED',       // Purple
};

export const AcademicTrendChart: React.FC<AcademicTrendChartProps> = ({
  data,
}) => {
  if (!data || data.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-6 shadow-soft-xs text-center select-none">
        <div className="w-12 h-12 rounded-xl bg-neutral-100 text-neutral-400 flex items-center justify-center mx-auto mb-3">
          <BarChart2 className="h-6 w-6" />
        </div>
        <h3 className="font-bold text-neutral-800 text-sm">Deneme Net Ortalaması</h3>
        <p className="text-xs text-neutral-400 mt-1">
          Henüz kurum genelinde tamamlanmış deneme sınavı sonucu bulunmuyor.
        </p>
      </div>
    );
  }

  const lastData = data[data.length - 1] || { turkce: 0, matematik: 0 };
  const firstData = data[0] || { turkce: 0, matematik: 0 };

  const turkceChange = lastData.turkce - firstData.turkce;
  const matChange = lastData.matematik - firstData.matematik;

  return (
    <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs select-none">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4 pb-3 border-b border-neutral-100">
        <div>
          <h2 className="font-bold text-navy-950 text-sm">Kurum Geneli TYT Net Ortalamaları</h2>
          <p className="text-xs text-neutral-400 mt-0.5">Gerçekleşen sınavların ders bazlı net değişimi</p>
        </div>
        <div className="flex items-center gap-3 text-xs text-neutral-600 flex-wrap font-medium">
          <span className="flex items-center gap-1.5">
            <span className="inline-block w-2.5 h-2.5 rounded-full bg-[#315FE8]" />
            Türkçe
          </span>
          <span className="flex items-center gap-1.5">
            <span className="inline-block w-2.5 h-2.5 rounded-full bg-[#059669]" />
            Matematik
          </span>
          <span className="flex items-center gap-1.5">
            <span className="inline-block w-2.5 h-2.5 rounded-full bg-[#D97706]" />
            Sosyal
          </span>
          <span className="flex items-center gap-1.5">
            <span className="inline-block w-2.5 h-2.5 rounded-full bg-[#7C3AED]" />
            Fen
          </span>
        </div>
      </div>

      <div className="w-full h-[220px]">
        <ResponsiveContainer width="100%" height="100%">
          <LineChart data={data} margin={{ top: 5, right: 10, bottom: 0, left: -15 }}>
            <CartesianGrid strokeDasharray="3 3" stroke="#F1F5F9" />
            <XAxis
              dataKey="ay"
              tick={{ fontSize: 11, fill: '#64748B' }}
              axisLine={false}
              tickLine={false}
            />
            <YAxis
              tick={{ fontSize: 11, fill: '#64748B' }}
              axisLine={false}
              tickLine={false}
              domain={[0, 'auto']}
            />
            <Tooltip
              contentStyle={{
                background: '#14233E',
                border: 'none',
                borderRadius: 12,
                fontSize: 12,
                color: '#F8F7F3',
                boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
              }}
              labelStyle={{ color: '#93C5FD', marginBottom: 4, fontWeight: 'bold' }}
              // eslint-disable-next-line @typescript-eslint/no-explicit-any
              formatter={(value: any) => [`${Number(value || 0).toFixed(1)} Net`, '']}
            />
            <Line
              type="monotone"
              dataKey="turkce"
              name="Türkçe"
              stroke={LINE_COLORS.turkce}
              strokeWidth={2.5}
              dot={{ r: 3.5, fill: LINE_COLORS.turkce }}
              activeDot={{ r: 5.5 }}
            />
            <Line
              type="monotone"
              dataKey="matematik"
              name="Matematik"
              stroke={LINE_COLORS.matematik}
              strokeWidth={2.5}
              dot={{ r: 3.5, fill: LINE_COLORS.matematik }}
              activeDot={{ r: 5.5 }}
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

      {data.length > 1 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 mt-3 pt-3 border-t border-neutral-100 text-xs">
          <div className="flex items-center gap-1.5 text-neutral-600">
            <TrendingUp className="h-3.5 w-3.5 text-emerald-600 shrink-0" />
            <span>
              Türkçe Değişimi:{' '}
              <strong className={turkceChange >= 0 ? 'text-emerald-700' : 'text-rose-600'}>
                {turkceChange >= 0 ? `+${turkceChange.toFixed(1)}` : turkceChange.toFixed(1)} Net
              </strong>
            </span>
          </div>
          <div className="flex items-center gap-1.5 text-neutral-600">
            <TrendingUp className="h-3.5 w-3.5 text-emerald-600 shrink-0" />
            <span>
              Matematik Değişimi:{' '}
              <strong className={matChange >= 0 ? 'text-emerald-700' : 'text-rose-600'}>
                {matChange >= 0 ? `+${matChange.toFixed(1)}` : matChange.toFixed(1)} Net
              </strong>
            </span>
          </div>
        </div>
      )}
    </div>
  );
};
