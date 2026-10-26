import React from 'react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';
import { ChevronRight } from 'lucide-react';
import {
  StudentSubjectPerformanceDto,
  SubjectTrendDto,
  SubjectClassificationDto,
} from '../types';

interface AcademicPerformanceCardProps {
  subjects: StudentSubjectPerformanceDto[];
  trends: SubjectTrendDto[];
  classifications?: SubjectClassificationDto[];
  onNavigateDetail?: () => void;
}

export const AcademicPerformanceCard: React.FC<AcademicPerformanceCardProps> = ({
  subjects,
  trends,
  onNavigateDetail,
}) => {
  // Sort subjects by total questions or exam count to pick top 3 for chart
  const subjectsWithData = subjects.filter((s) => s.examCount > 0);
  const chartSubjects = subjectsWithData.slice(0, 3);

  // Build chart points from first subject with timeSeries
  const primaryTrend = trends.find((t) => t.timeSeries && t.timeSeries.length > 0) || trends[0];
  const chartObservations = (primaryTrend?.timeSeries || []).slice(-5);

  const chartData = chartObservations.map((obs, idx) => {
    const point: any = {
      name: `${idx + 1}. Deneme`,
      date: new Date(obs.takenAt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' }),
    };

    // Populate net values for top 3 subjects
    chartSubjects.forEach((sub) => {
      const subTrend = trends.find((t) => t.subjectId === sub.subjectId);
      const subObs = subTrend?.timeSeries?.[idx] || subTrend?.timeSeries?.slice(-5)[idx];
      point[sub.subjectName] = subObs && subObs.totalQuestions > 0 ? Math.round(subObs.net * 10) / 10 : null;
    });

    return point;
  });

  const subjectLineColors = ['#315FE8', '#10B981', '#F59E0B', '#8B5CF6'];

  return (
    <div className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70">
      <div className="flex items-center justify-between mb-2">
        <h3 className="text-sm font-bold text-[#14233E]">Performansım ve Gelişim</h3>
        {onNavigateDetail && (
          <button
            type="button"
            onClick={onNavigateDetail}
            className="text-xs font-semibold text-[#315FE8] hover:underline inline-flex items-center gap-0.5"
          >
            Tümünü Gör <ChevronRight className="w-3.5 h-3.5" />
          </button>
        )}
      </div>

      <div className="flex items-center justify-between text-xs text-neutral-400 mb-3">
        <span>Son Deneme Netleri</span>
        <div className="flex items-center gap-3">
          {chartSubjects.map((sub, idx) => (
            <div key={sub.subjectId} className="flex items-center gap-1.5">
              <span
                className="w-2 h-2 rounded-full"
                style={{ backgroundColor: subjectLineColors[idx % subjectLineColors.length] }}
              />
              <span className="text-[11px] font-medium text-neutral-600">{sub.subjectName}</span>
            </div>
          ))}
        </div>
      </div>

      {chartData.length > 0 ? (
        <div className="h-44 sm:h-52 w-full -ml-2">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={chartData} margin={{ top: 10, right: 10, left: -20, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#F1F5F9" />
              <XAxis
                dataKey="name"
                tick={{ fontSize: 10, fill: '#94A3B8' }}
                axisLine={false}
                tickLine={false}
              />
              <YAxis
                tick={{ fontSize: 10, fill: '#94A3B8' }}
                axisLine={false}
                tickLine={false}
                domain={['auto', 'auto']}
              />
              <Tooltip
                formatter={(val: any) => [val !== null ? `${val} net` : 'Veri yok', '']}
                contentStyle={{
                  borderRadius: '12px',
                  border: '1px solid #E2E8F0',
                  boxShadow: '0 4px 12px rgba(0,0,0,0.08)',
                  fontSize: '11px',
                }}
              />
              {chartSubjects.map((sub, idx) => (
                <Line
                  key={sub.subjectId}
                  type="monotone"
                  dataKey={sub.subjectName}
                  stroke={subjectLineColors[idx % subjectLineColors.length]}
                  strokeWidth={2.5}
                  connectNulls={false}
                  dot={{ r: 3, fill: subjectLineColors[idx % subjectLineColors.length] }}
                  activeDot={{ r: 5 }}
                />
              ))}
            </LineChart>
          </ResponsiveContainer>
        </div>
      ) : (
        <div className="h-44 flex items-center justify-center text-xs text-neutral-400 bg-neutral-50/50 rounded-xl">
          Gelişim trendi için en az bir deneme verisi gereklidir.
        </div>
      )}

      {/* Subject Metric Badges Bottom Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 mt-4 pt-3 border-t border-neutral-100">
        {subjects.slice(0, 4).map((sub) => {
          const hasExamData = sub.totalQuestions > 0 && sub.attemptCount > 0;
          const successPct = Math.round((sub.successRate ?? 0) * 100);
          const avgNetPerAttempt = hasExamData
            ? Math.round((sub.totalNet / sub.attemptCount) * 10) / 10
            : null;

          return (
            <div key={sub.subjectId} className="bg-neutral-50/60 rounded-xl p-2.5 text-center">
              <span className="text-[11px] font-medium text-neutral-500 block truncate">
                {sub.subjectName}
              </span>
              <span className="text-sm sm:text-base font-bold text-[#14233E] mt-0.5 block">
                {hasExamData ? `%${successPct}` : 'Veri Yok'}
              </span>
              <span className="text-[10px] text-neutral-400 block mt-0.5">
                {hasExamData
                  ? `Ort. ${avgNetPerAttempt} net`
                  : 'Soru çözülmedi'}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
};
