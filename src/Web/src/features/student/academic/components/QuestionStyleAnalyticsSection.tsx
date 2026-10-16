import React, { useEffect, useState } from 'react';
import {
  HelpCircle,
  TrendingUp,
  TrendingDown,
  Minus,
  AlertCircle,
  Layers,
} from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import {
  StudentQuestionStyleAnalyticsDto,
  formatRate,
  getClassificationLabel,
  getClassificationBadgeVariant,
  getTrendDirectionLabel,
} from '../types';
import { academicService } from '@/services/academicService';

interface QuestionStyleAnalyticsSectionProps {
  studentId: string;
  isReadOnly?: boolean;
  simplified?: boolean;
}

export const QuestionStyleAnalyticsSection: React.FC<QuestionStyleAnalyticsSectionProps> = ({
  studentId,
  simplified = false,
}) => {
  const [styles, setStyles] = useState<StudentQuestionStyleAnalyticsDto[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let isMounted = true;
    const controller = new AbortController();

    async function fetchStyleAnalytics() {
      try {
        setLoading(true);
        setError(null);
        const data = await academicService.getQuestionStyleAnalytics(
          studentId,
          undefined,
          controller.signal
        );
        if (isMounted) {
          setStyles(data || []);
        }
      } catch (err: unknown) {
        if (isMounted) {
          // Ignore abort errors
          if (err instanceof DOMException && err.name === 'AbortError') return;
          setError('Soru tarzı analizi yüklenirken bir sorun oluştu.');
        }
      } finally {
        if (isMounted) {
          setLoading(false);
        }
      }
    }

    if (studentId) {
      fetchStyleAnalytics();
    }

    return () => {
      isMounted = false;
      controller.abort();
    };
  }, [studentId]);

  if (loading) {
    return (
      <Card className="p-4 sm:p-6 bg-white border border-neutral-100">
        <div className="flex items-center space-x-3 mb-4">
          <div className="h-5 w-5 bg-neutral-200 animate-pulse rounded" />
          <div className="h-5 w-48 bg-neutral-200 animate-pulse rounded" />
        </div>
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-32 bg-neutral-50 animate-pulse rounded-lg border border-neutral-100" />
          ))}
        </div>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="p-4 sm:p-6 bg-rose-50/50 border border-rose-100">
        <div className="flex items-center space-x-2 text-rose-800 text-sm">
          <AlertCircle className="h-4 w-4 shrink-0 text-rose-600" />
          <span>{error}</span>
        </div>
      </Card>
    );
  }

  const hasData = styles.some((s) => s.totalQuestions > 0);

  const renderTrendIcon = (trend?: 'Increasing' | 'Stable' | 'Decreasing' | null) => {
    switch (trend) {
      case 'Increasing':
        return <TrendingUp className="h-3.5 w-3.5 text-emerald-600" />;
      case 'Decreasing':
        return <TrendingDown className="h-3.5 w-3.5 text-rose-600" />;
      case 'Stable':
        return <Minus className="h-3.5 w-3.5 text-sky-600" />;
      default:
        return <HelpCircle className="h-3.5 w-3.5 text-neutral-400" />;
    }
  };

  return (
    <Card className="p-4 sm:p-6 bg-white border border-neutral-100">
      <div className="flex items-center justify-between mb-4 pb-2 border-b border-neutral-100">
        <div className="flex items-center space-x-2">
          <Layers className="h-5 w-5 text-primary-600" />
          <div>
            <h3 className="text-base font-semibold text-neutral-900">
              {simplified ? 'Soru Tarzı Başarımı' : 'Soru Tarzı Performans Analizi'}
            </h3>
            <p className="text-xs text-neutral-500">
              {simplified
                ? 'Öğrencinin farklı soru formatlarındaki performansı'
                : 'Yeni nesil, işlem, grafik/tablo ve diğer soru tarzlarındaki deterministik başarım'}
            </p>
          </div>
        </div>
        {!hasData && (
          <Badge variant="secondary" size="sm">
            Veri Bekleniyor
          </Badge>
        )}
      </div>

      {!hasData ? (
        <div className="text-center py-8 px-4 bg-neutral-50/70 rounded-lg border border-neutral-100">
          <HelpCircle className="h-8 w-8 text-neutral-400 mx-auto mb-2" />
          <p className="text-sm font-medium text-neutral-700">Henüz Soru Tarzı Verisi Yok</p>
          <p className="text-xs text-neutral-500 mt-1 max-w-md mx-auto">
            Soru tarzı analizi için öğrencinin soru-düzeyinde etiketli denemeleri tamamlaması gerekmektedir (en az 2 deneme ve 5 soru).
          </p>
        </div>
      ) : simplified ? (
        // Simplified view for parent portal
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          {styles.map((style) => (
            <div
              key={style.questionStyleId}
              className="p-3.5 rounded-lg border border-neutral-100 bg-neutral-50/50 flex flex-col justify-between"
            >
              <div>
                <div className="flex items-center justify-between gap-2">
                  <span className="text-sm font-medium text-neutral-900 truncate">
                    {style.name}
                  </span>
                  <Badge variant={getClassificationBadgeVariant(style.classificationState)} size="sm">
                    {getClassificationLabel(style.classificationState)}
                  </Badge>
                </div>
                <div className="mt-2 flex items-baseline justify-between">
                  <span className="text-xl font-bold text-neutral-900">
                    {formatRate(style.successRate)}
                  </span>
                  <span className="text-xs text-neutral-500">
                    {style.totalQuestions} soru
                  </span>
                </div>
              </div>
              <div className="mt-3 pt-2 border-t border-neutral-200/50 flex items-center justify-between text-[11px] text-neutral-500">
                <span>Doğru: {style.correctCount} / {style.attemptedQuestions}</span>
                {style.trendDirection && (
                  <span className="flex items-center gap-1 font-medium">
                    {renderTrendIcon(style.trendDirection)}
                    {getTrendDirectionLabel(style.trendDirection)}
                  </span>
                )}
              </div>
            </div>
          ))}
        </div>
      ) : (
        // Full detailed view for student & coach
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-3.5">
          {styles.map((style) => (
            <div
              key={style.questionStyleId}
              className="p-4 rounded-xl border border-neutral-200/70 bg-white hover:border-primary-200 transition-all shadow-xs flex flex-col justify-between"
            >
              <div>
                <div className="flex items-start justify-between gap-2 mb-2">
                  <h4 className="text-sm font-semibold text-neutral-900 truncate" title={style.name}>
                    {style.name}
                  </h4>
                  <Badge variant={getClassificationBadgeVariant(style.classificationState)} size="sm">
                    {getClassificationLabel(style.classificationState)}
                  </Badge>
                </div>

                <div className="grid grid-cols-2 gap-2 my-3 p-2.5 bg-neutral-50 rounded-lg">
                  <div>
                    <span className="text-[10px] font-medium text-neutral-500 uppercase tracking-wider block">
                      Başarı Oranı
                    </span>
                    <span className="text-lg font-bold text-neutral-900">
                      {formatRate(style.successRate)}
                    </span>
                  </div>
                  <div>
                    <span className="text-[10px] font-medium text-neutral-500 uppercase tracking-wider block">
                      Doğruluk
                    </span>
                    <span className="text-lg font-bold text-neutral-900">
                      {formatRate(style.accuracy)}
                    </span>
                  </div>
                </div>

                <div className="space-y-1.5 text-xs text-neutral-600">
                  <div className="flex justify-between">
                    <span>Toplam Soru:</span>
                    <span className="font-medium text-neutral-900">{style.totalQuestions}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Cevaplanan:</span>
                    <span className="font-medium text-neutral-900">{style.attemptedQuestions}</span>
                  </div>
                  <div className="flex justify-between text-[11px] text-neutral-500 pt-1 border-t border-neutral-100">
                    <span className="text-emerald-700">D: {style.correctCount}</span>
                    <span className="text-rose-700">Y: {style.incorrectCount}</span>
                    <span className="text-neutral-500">B: {style.blankCount}</span>
                    <span>Kapsam: {formatRate(style.coverage)}</span>
                  </div>
                </div>
              </div>

              <div className="mt-3.5 pt-2.5 border-t border-neutral-100 flex items-center justify-between text-xs">
                <span className="text-neutral-500 text-[11px]">
                  {style.observationCount} deneme
                </span>
                <div className="flex items-center gap-1 font-medium text-[11px] text-neutral-700">
                  {renderTrendIcon(style.trendDirection)}
                  <span>{getTrendDirectionLabel(style.trendDirection ?? null)}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </Card>
  );
};
