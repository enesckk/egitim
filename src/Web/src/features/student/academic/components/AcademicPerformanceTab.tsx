import React, { useState } from 'react';
import {
  ResponsiveContainer,
  LineChart,
  Line,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
} from 'recharts';
import { TrendingUp, TrendingDown, Minus, BookOpen, Layers } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import {
  StudentSubjectPerformanceDto,
  SubjectTrendDto,
  SubjectClassificationDto,
  formatRate,
  getClassificationLabel,
  getClassificationBadgeVariant,
  getTrendDirectionLabel,
} from '../types';

interface AcademicPerformanceTabProps {
  subjects: StudentSubjectPerformanceDto[];
  trends: SubjectTrendDto[];
  classifications: SubjectClassificationDto[];
}

export const AcademicPerformanceTab: React.FC<AcademicPerformanceTabProps> = ({
  subjects,
  trends,
  classifications,
}) => {
  const [selectedSubjectId, setSelectedSubjectId] = useState<string>(
    subjects.length > 0 ? subjects[0].subjectId : ''
  );

  const selectedSubject = subjects.find((s) => s.subjectId === selectedSubjectId) || subjects[0];
  const selectedTrend = trends.find((t) => t.subjectId === selectedSubjectId);
  const selectedClassification = classifications.find((c) => c.subjectId === selectedSubjectId);

  // Transform observations for Recharts chronological ordering
  const chartData = (selectedTrend?.timeSeries || []).map((obs) => ({
    takenAtFormatted: new Date(obs.takenAt).toLocaleDateString('tr-TR', {
      day: 'numeric',
      month: 'short',
    }),
    examTitle: obs.examTitle,
    successRatePct: Math.round(obs.successRate * 100),
    accuracyPct: Math.round(obs.accuracy * 100),
    net: obs.net,
    totalQuestions: obs.totalQuestions,
  }));

  const renderTrendIcon = (direction: string | null | undefined) => {
    if (direction === 'Increasing')
      return <TrendingUp className="h-4 w-4 text-emerald-600 inline" />;
    if (direction === 'Decreasing')
      return <TrendingDown className="h-4 w-4 text-rose-600 inline" />;
    return <Minus className="h-4 w-4 text-neutral-400 inline" />;
  };

  return (
    <div className="space-y-4 sm:space-y-6">
      {/* Subject Selector Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
        {subjects.map((sub) => {
          const classif = classifications.find((c) => c.subjectId === sub.subjectId);
          const isSelected = sub.subjectId === selectedSubjectId;

          return (
            <Card
              key={sub.subjectId}
              onClick={() => setSelectedSubjectId(sub.subjectId)}
              className={`p-4 border transition-all cursor-pointer ${
                isSelected
                  ? 'border-primary-500 ring-2 ring-primary-100 bg-primary-50/20'
                  : 'border-neutral-100 bg-white hover:border-neutral-200'
              }`}
            >
              <div className="flex items-start justify-between">
                <div>
                  <h4 className="font-semibold text-neutral-900 text-sm sm:text-base">
                    {sub.subjectName}
                  </h4>
                  <p className="text-xs text-neutral-500 mt-0.5">
                    {sub.examCount} Sınav • {sub.totalQuestions} Soru
                  </p>
                </div>
                {classif && (
                  <Badge variant={getClassificationBadgeVariant(classif.state)}>
                    {getClassificationLabel(classif.state)}
                  </Badge>
                )}
              </div>

              <div className="grid grid-cols-3 gap-2 mt-4 pt-3 border-t border-neutral-100 text-xs">
                <div>
                  <span className="text-neutral-400 block">Başarı Oranı</span>
                  <span className="font-semibold text-neutral-900">
                    {formatRate(sub.successRate)}
                  </span>
                </div>
                <div>
                  <span className="text-neutral-400 block">Doğruluk</span>
                  <span className="font-semibold text-neutral-900">
                    {formatRate(sub.accuracy)}
                  </span>
                </div>
                <div>
                  <span className="text-neutral-400 block">Kapsam</span>
                  <span className="font-semibold text-neutral-900">
                    {formatRate(sub.coverage)}
                  </span>
                </div>
              </div>
            </Card>
          );
        })}
      </div>

      {/* Selected Subject Trends & Details Section */}
      {selectedSubject && (
        <Card className="p-4 sm:p-6 bg-white border border-neutral-100 space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-neutral-100 pb-3">
            <div>
              <div className="flex items-center gap-2">
                <BookOpen className="h-5 w-5 text-primary-600" />
                <h3 className="text-lg font-bold text-neutral-900">
                  {selectedSubject.subjectName} Gelişim Trendi
                </h3>
              </div>
              <p className="text-xs text-neutral-500 mt-0.5">
                Sınav tarihine (TakenAt) göre kronolojik başarı değişimi
              </p>
            </div>

            {selectedClassification && (
              <div className="flex items-center gap-2">
                <span className="text-xs text-neutral-500">Trend Yönü:</span>
                <span className="inline-flex items-center gap-1 text-xs font-semibold text-neutral-800 bg-neutral-100 px-2.5 py-1 rounded-full">
                  {renderTrendIcon(selectedClassification.trendDirection)}
                  {getTrendDirectionLabel(selectedClassification.trendDirection)}
                </span>
              </div>
            )}
          </div>

          {/* Time Series Chart */}
          {chartData.length > 0 ? (
            <div className="h-64 sm:h-72 w-full pt-2">
              <ResponsiveContainer width="100%" height="100%">
                <LineChart data={chartData} margin={{ top: 10, right: 20, left: -20, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="#f1f5f9" />
                  <XAxis dataKey="takenAtFormatted" tick={{ fontSize: 11, fill: '#64748b' }} />
                  <YAxis domain={[0, 100]} tick={{ fontSize: 11, fill: '#64748b' }} unit="%" />
                  <Tooltip
                    content={({ active, payload }) => {
                      if (active && payload && payload.length) {
                        const data = payload[0].payload;
                        return (
                          <div className="bg-neutral-900 text-white text-xs p-3 rounded-xl shadow-lg border border-neutral-800 space-y-1">
                            <p className="font-semibold text-neutral-200">{data.examTitle}</p>
                            <p className="text-primary-300 font-bold">
                              Başarı Oranı: %{data.successRatePct}
                            </p>
                            <p className="text-neutral-400">
                              Doğruluk Oranı: %{data.accuracyPct} | Net: {data.net} / {data.totalQuestions}
                            </p>
                          </div>
                        );
                      }
                      return null;
                    }}
                  />
                  <Line
                    type="monotone"
                    dataKey="successRatePct"
                    stroke="#2563eb"
                    strokeWidth={3}
                    dot={{ r: 4, fill: '#2563eb', strokeWidth: 2, stroke: '#ffffff' }}
                    activeDot={{ r: 6, fill: '#1d4ed8' }}
                  />
                </LineChart>
              </ResponsiveContainer>
            </div>
          ) : (
            <div className="py-12 text-center text-xs text-neutral-500 bg-neutral-50 rounded-xl border border-neutral-100">
              <Layers className="h-8 w-8 text-neutral-300 mx-auto mb-2" />
              Bu ders için henüz zaman serisi trend verisi oluşmadı.
            </div>
          )}
        </Card>
      )}
    </div>
  );
};
