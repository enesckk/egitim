import React from 'react';
import { Sparkles, Award, ArrowRight } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import {
  AcademicRecommendationDto,
  formatRate,
  getClassificationLabel,
  getClassificationBadgeVariant,
  getRecommendationTypeLabel,
  getTrendDirectionLabel,
} from '../types';

interface AcademicPrioritiesTabProps {
  recommendations: AcademicRecommendationDto[];
  onNavigateToStudyPlan: () => void;
}

export const AcademicPrioritiesTab: React.FC<AcademicPrioritiesTabProps> = ({
  recommendations,
  onNavigateToStudyPlan,
}) => {
  if (recommendations.length === 0) {
    return (
      <Card className="p-8 text-center bg-white border border-neutral-100">
        <Award className="h-10 w-10 text-emerald-500 mx-auto mb-3" />
        <h3 className="text-base font-semibold text-neutral-900">
          Öncelikli Kritik Eksik Bulunmuyor
        </h3>
        <p className="text-xs text-neutral-500 mt-1 max-w-md mx-auto">
          Tüm değerlendirilen akademik alanlarınızda başarımınız yeterli seviyede. Düzenli soru çözerek ve deneme sınavlarına katılarak seviyenizi koruyabilirsiniz.
        </p>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header Callout */}
      <div className="bg-primary-50/50 border border-primary-100 p-4 rounded-xl flex items-center justify-between">
        <div className="flex items-center gap-2.5">
          <Sparkles className="h-5 w-5 text-primary-600 shrink-0" />
          <div>
            <h3 className="text-sm font-bold text-primary-950">
              Sizin İçin Sıralanmış Akademik Öncelikler
            </h3>
            <p className="text-xs text-primary-700 mt-0.5">
              Son sınav verilerinize göre başarım ve trend faktörleri analiz edilerek sıralanmıştır.
            </p>
          </div>
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={onNavigateToStudyPlan}
          rightIcon={<ArrowRight className="h-4 w-4" />}
          className="text-xs shrink-0 hidden sm:flex"
        >
          Çalışma Planına Aktar
        </Button>
      </div>

      {/* Recommendations List */}
      <div className="space-y-3">
        {recommendations.map((rec, index) => {
          const breadcrumb =
            rec.nodeType === 'QuestionStyle'
              ? 'Soru Tarzı Odağı'
              : [rec.subjectName, rec.topicName, rec.subTopicName]
                  .filter(Boolean)
                  .join(' → ');

          return (
            <Card
              key={`${rec.nodeType}-${rec.nodeId}`}
              className="p-4 sm:p-5 bg-white border border-neutral-100 hover:border-neutral-200 transition-colors shadow-soft-sm space-y-3"
            >
              <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-2">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="w-5 h-5 rounded-full bg-primary-100 text-primary-700 text-xs font-bold flex items-center justify-center">
                      {index + 1}
                    </span>
                    <h4 className="font-bold text-neutral-900 text-base">{rec.name}</h4>
                    <span className="text-xs text-neutral-400 font-mono">({rec.code})</span>
                  </div>
                  {breadcrumb && (
                    <p className="text-xs text-neutral-500 pl-7">{breadcrumb}</p>
                  )}
                </div>

                <div className="flex items-center gap-2 self-start sm:self-auto pl-7 sm:pl-0">
                  <Badge variant={getClassificationBadgeVariant(rec.classificationState)}>
                    {getClassificationLabel(rec.classificationState)}
                  </Badge>
                  <span className="text-xs text-neutral-600 bg-neutral-100 px-2.5 py-1 rounded-full font-medium">
                    {getRecommendationTypeLabel(rec.recommendationType)}
                  </span>
                </div>
              </div>

              <div className="grid grid-cols-2 sm:grid-cols-4 gap-2 text-xs bg-neutral-50 p-3 rounded-xl border border-neutral-100/80">
                <div>
                  <span className="text-neutral-400 block">Başarı Oranı</span>
                  <span className="font-semibold text-neutral-900">
                    {formatRate(rec.successRate)}
                  </span>
                </div>
                <div>
                  <span className="text-neutral-400 block">Soru Sayısı</span>
                  <span className="font-semibold text-neutral-900">
                    {rec.totalQuestions} Soru
                  </span>
                </div>
                <div>
                  <span className="text-neutral-400 block">Trend</span>
                  <span className="font-semibold text-neutral-900">
                    {getTrendDirectionLabel(rec.trendDirection)}
                  </span>
                </div>
                <div>
                  <span className="text-neutral-400 block">Öncelik Tipi</span>
                  <span className="font-semibold text-neutral-900">
                    {getRecommendationTypeLabel(rec.recommendationType)}
                  </span>
                </div>
              </div>
            </Card>
          );
        })}
      </div>
    </div>
  );
};
