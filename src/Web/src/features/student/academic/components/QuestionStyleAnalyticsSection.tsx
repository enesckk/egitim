import React, { useState, useEffect } from 'react';
import {
  ChevronRight,
  TrendingUp,
  TrendingDown,
  Minus,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import {
  StudentQuestionStyleAnalyticsDto,
  getTrendDirectionLabel,
} from '../types';
import { academicService } from '@/services/academicService';

export interface QuestionStyleAnalyticsSectionProps {
  styles?: StudentQuestionStyleAnalyticsDto[];
  studentId?: string;
  isReadOnly?: boolean;
  simplified?: boolean;
  loading?: boolean;
  onNavigateDetail?: () => void;
}

export const QuestionStyleAnalyticsSection: React.FC<QuestionStyleAnalyticsSectionProps> = ({
  styles: initialStyles,
  studentId,
  simplified = false,
  loading: externalLoading = false,
  onNavigateDetail,
}) => {
  const [internalStyles, setInternalStyles] = useState<StudentQuestionStyleAnalyticsDto[]>(initialStyles || []);
  const [internalLoading, setInternalLoading] = useState<boolean>(!initialStyles && !!studentId);
  const [showAllModal, setShowAllModal] = useState(false);

  useEffect(() => {
    if (initialStyles) {
      setInternalStyles(initialStyles);
      return;
    }

    if (studentId) {
      let isMounted = true;
      const controller = new AbortController();

      async function fetchStyles() {
        try {
          setInternalLoading(true);
          const data = await academicService.getQuestionStyleAnalytics(
            studentId!,
            undefined,
            controller.signal
          );
          if (isMounted) {
            setInternalStyles(data || []);
          }
        } catch {
          // ignore error
        } finally {
          if (isMounted) {
            setInternalLoading(false);
          }
        }
      }

      fetchStyles();
      return () => {
        isMounted = false;
        controller.abort();
      };
    }
  }, [initialStyles, studentId]);

  const effectiveLoading = externalLoading || internalLoading;
  const effectiveStyles = initialStyles || internalStyles;

  if (effectiveLoading && effectiveStyles.length === 0) {
    return (
      <div className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 animate-pulse">
        <div className="h-5 w-40 bg-neutral-200 rounded mb-4" />
        <div className="space-y-3">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="h-6 bg-neutral-100 rounded" />
          ))}
        </div>
      </div>
    );
  }

  // Filter styles with attempts first, sort by successRate ascending (weakest / most challenged first)
  const stylesWithData = effectiveStyles.filter((s) => s.totalQuestions > 0);
  const sortedStyles = [...stylesWithData].sort((a, b) => (a.successRate ?? 0) - (b.successRate ?? 0));
  const topStyles = simplified ? sortedStyles.slice(0, 3) : sortedStyles.slice(0, 4);

  const renderTrendIcon = (trend?: 'Increasing' | 'Stable' | 'Decreasing' | null) => {
    switch (trend) {
      case 'Increasing':
        return <TrendingUp className="w-3.5 h-3.5 text-emerald-600 inline" />;
      case 'Decreasing':
        return <TrendingDown className="w-3.5 h-3.5 text-rose-600 inline" />;
      case 'Stable':
        return <Minus className="w-3.5 h-3.5 text-sky-600 inline" />;
      default:
        return null;
    }
  };

  return (
    <div className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 flex flex-col justify-between">
      <div>
        <div className="flex items-center justify-between mb-1">
          <h3 className="text-sm font-bold text-[#14233E]">
            {simplified ? 'Soru Tarzı Başarımı' : 'Soru Tarzı Analizim'}
          </h3>
          {onNavigateDetail ? (
            <button
              type="button"
              onClick={onNavigateDetail}
              className="text-xs font-semibold text-[#315FE8] hover:underline inline-flex items-center gap-0.5"
            >
              Detaylı Analiz <ChevronRight className="w-3.5 h-3.5" />
            </button>
          ) : (
            <button
              type="button"
              onClick={() => setShowAllModal(true)}
              className="text-xs font-semibold text-[#315FE8] hover:underline inline-flex items-center gap-0.5"
            >
              Detaylı Analiz <ChevronRight className="w-3.5 h-3.5" />
            </button>
          )}
        </div>
        <p className="text-[11px] text-neutral-400 mb-4">
          {simplified
            ? 'Farklı soru tarzlarındaki başarım oranları'
            : 'En çok zorlandığın soru formatları'}
        </p>

        {stylesWithData.length === 0 ? (
          <div className="py-6 text-center text-xs text-neutral-400">
            Henüz soru tarzı analizi için yeterli veri bulunmuyor.
          </div>
        ) : (
          <div className="space-y-3.5">
            {topStyles.map((style) => {
              const rate = Math.round((style.successRate ?? 0) * 100);
              const barColor =
                rate >= 70
                  ? 'bg-emerald-500'
                  : rate >= 50
                  ? 'bg-sky-500'
                  : rate >= 35
                  ? 'bg-amber-500'
                  : 'bg-rose-500';

              return (
                <div key={style.questionStyleId} className="flex items-center gap-3">
                  <div className="w-24 sm:w-28 text-xs font-medium text-neutral-700 truncate" title={style.name}>
                    {style.name}
                  </div>
                  <div className="flex-1 h-2 rounded-full bg-neutral-100 overflow-hidden">
                    <div
                      className={cn('h-full rounded-full transition-all duration-500', barColor)}
                      style={{ width: `${Math.max(4, rate)}%` }}
                    />
                  </div>
                  <div className="w-10 text-right text-xs font-bold text-neutral-800">
                    %{rate}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      <div className="mt-4 pt-3 border-t border-neutral-100 flex items-center justify-between text-[11px] text-neutral-400">
        <span>{stylesWithData.length} farklı soru tarzı değerlendirildi</span>
        {onNavigateDetail && (
          <button
            type="button"
            onClick={onNavigateDetail}
            className="text-neutral-600 font-semibold hover:text-[#315FE8] inline-flex items-center gap-0.5"
          >
            Tümünü İncele
          </button>
        )}
      </div>

      {/* Progressive Modal for All Question Styles */}
      {showAllModal && (
        <div className="fixed inset-0 z-50 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-2xl w-full max-h-[85vh] flex flex-col shadow-2xl border border-neutral-200">
            <div className="p-5 border-b border-neutral-100 flex items-center justify-between">
              <div>
                <h3 className="text-base font-bold text-[#14233E]">Tüm Soru Tarzı Analizi</h3>
                <p className="text-xs text-neutral-400 mt-0.5">Soru formatlarındaki doğruluk ve gelişim trendleri</p>
              </div>
              <button
                type="button"
                onClick={() => setShowAllModal(false)}
                className="w-8 h-8 rounded-full bg-neutral-100 text-neutral-600 hover:bg-neutral-200 flex items-center justify-center"
              >
                ✕
              </button>
            </div>

            <div className="p-5 overflow-y-auto space-y-3">
              {effectiveStyles.map((style) => (
                <div key={style.questionStyleId} className="p-3.5 rounded-2xl border border-neutral-100 bg-neutral-50/50 flex items-center justify-between">
                  <div>
                    <h4 className="text-xs font-bold text-neutral-900">{style.name}</h4>
                    <p className="text-[11px] text-neutral-400 mt-0.5">
                      {style.totalQuestions} soru • {style.correctCount} doğru
                    </p>
                  </div>
                  <div className="text-right">
                    <span className="text-sm font-bold text-[#14233E]">
                      %{Math.round((style.successRate ?? 0) * 100)}
                    </span>
                    {style.trendDirection && (
                      <p className="text-[10px] text-neutral-500 flex items-center justify-end gap-1 mt-0.5">
                        {renderTrendIcon(style.trendDirection)}
                        {getTrendDirectionLabel(style.trendDirection)}
                      </p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
