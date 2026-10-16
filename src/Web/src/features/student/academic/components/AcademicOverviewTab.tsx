import React from 'react';
import {
  BookOpen,
  Award,
  AlertTriangle,
  TrendingUp,
  Target,
  Sparkles,
  Calendar,
} from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import {
  StudentKnowledgeMapDto,
  AcademicRecommendationDto,
  AcademicStudyPlanDto,
  formatRate,
  getClassificationLabel,
  getClassificationBadgeVariant,
  getRecommendationTypeLabel,
} from '../types';

import { AcademicAiSummaryCard } from './AcademicAiSummaryCard';
import { QuestionStyleAnalyticsSection } from './QuestionStyleAnalyticsSection';

interface AcademicOverviewTabProps {
  studentId?: string;
  knowledgeMap: StudentKnowledgeMapDto | null;
  recommendations: AcademicRecommendationDto[];
  studyPlan: AcademicStudyPlanDto | null;
  onNavigateTab: (tab: 'performance' | 'knowledge-map' | 'priorities' | 'study-plan') => void;
}

export const AcademicOverviewTab: React.FC<AcademicOverviewTabProps> = ({
  studentId,
  knowledgeMap,
  recommendations,
  studyPlan,
  onNavigateTab,
}) => {
  const summary = knowledgeMap?.summary;
  const topRecommendation = recommendations.length > 0 ? recommendations[0] : null;

  return (
    <div className="space-y-4 sm:space-y-6">
      {studentId && (
        <AcademicAiSummaryCard studentId={studentId} audience="student" />
      )}
      {/* 1. Academic State Counters Grid */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-3">
        <Card className="p-3.5 sm:p-4 bg-white border border-neutral-100 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs text-neutral-500 font-medium">Değerlendirilen Ders</span>
            <BookOpen className="h-4 w-4 text-primary-500" />
          </div>
          <div className="mt-2">
            <p className="text-xl sm:text-2xl font-bold text-neutral-900">
              {summary?.subjectsWithEvidence ?? 0}
              <span className="text-xs font-normal text-neutral-400 ml-1">/ {summary?.totalSubjects ?? 0}</span>
            </p>
            <p className="text-[11px] text-neutral-500 mt-0.5">Toplam Müfredat</p>
          </div>
        </Card>

        <Card className="p-3.5 sm:p-4 bg-emerald-50/50 border border-emerald-100 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs text-emerald-800 font-medium">Güçlü</span>
            <Award className="h-4 w-4 text-emerald-600" />
          </div>
          <div className="mt-2">
            <p className="text-xl sm:text-2xl font-bold text-emerald-900">
              {summary?.strongSubjects ?? 0}
            </p>
            <p className="text-[11px] text-emerald-700 mt-0.5">Yüksek Başarım</p>
          </div>
        </Card>

        <Card className="p-3.5 sm:p-4 bg-sky-50/50 border border-sky-100 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs text-sky-800 font-medium">Gelişiyor</span>
            <TrendingUp className="h-4 w-4 text-sky-600" />
          </div>
          <div className="mt-2">
            <p className="text-xl sm:text-2xl font-bold text-sky-900">
              {(summary?.totalSubjects ?? 0) -
                ((summary?.strongSubjects ?? 0) +
                  (summary?.weakSubjects ?? 0) +
                  (summary?.criticalSubjects ?? 0))}
            </p>
            <p className="text-[11px] text-sky-700 mt-0.5">Pekiştirilebilir</p>
          </div>
        </Card>

        <Card className="p-3.5 sm:p-4 bg-amber-50/50 border border-amber-100 flex flex-col justify-between">
          <div className="flex items-center justify-between">
            <span className="text-xs text-amber-800 font-medium">Geliştirilmeli</span>
            <Target className="h-4 w-4 text-amber-600" />
          </div>
          <div className="mt-2">
            <p className="text-xl sm:text-2xl font-bold text-amber-900">
              {summary?.weakSubjects ?? 0}
            </p>
            <p className="text-[11px] text-amber-700 mt-0.5">Tekrar Gerekli</p>
          </div>
        </Card>

        <Card className="p-3.5 sm:p-4 bg-rose-50/50 border border-rose-100 flex flex-col justify-between col-span-2 lg:col-span-1">
          <div className="flex items-center justify-between">
            <span className="text-xs text-rose-800 font-medium">Kritik</span>
            <AlertTriangle className="h-4 w-4 text-rose-600" />
          </div>
          <div className="mt-2">
            <p className="text-xl sm:text-2xl font-bold text-rose-900">
              {summary?.criticalSubjects ?? 0}
            </p>
            <p className="text-[11px] text-rose-700 mt-0.5">Acil Öncelikli</p>
          </div>
        </Card>
      </div>

      {/* 2. Question Style Analytics Section */}
      {studentId && (
        <QuestionStyleAnalyticsSection studentId={studentId} />
      )}

      {/* 3. Top Recommendation Callout & Proposed Plan Hero */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Highest Priority Recommendation Card */}
        <Card className="p-4 sm:p-5 bg-white border border-neutral-100 flex flex-col justify-between space-y-4">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-primary-700 bg-primary-50 px-2.5 py-1 rounded-full border border-primary-100">
                <Sparkles className="h-3.5 w-3.5 text-primary-600" />
                En Yüksek Öncelikli Akademik Odak
              </span>
              {topRecommendation && (
                <Badge variant={getClassificationBadgeVariant(topRecommendation.classificationState)}>
                  {getClassificationLabel(topRecommendation.classificationState)}
                </Badge>
              )}
            </div>

            {topRecommendation ? (
              <div className="space-y-1.5 pt-1">
                <h3 className="text-lg font-bold text-neutral-900 tracking-tight">
                  {topRecommendation.name}
                </h3>
                <p className="text-xs text-neutral-500">
                  {[
                    topRecommendation.subjectName,
                    topRecommendation.topicName,
                    topRecommendation.subTopicName,
                  ]
                    .filter(Boolean)
                    .join(' → ')}
                </p>
                <div className="flex items-center gap-4 text-xs text-neutral-600 pt-2 border-t border-neutral-100 mt-3">
                  <div>
                    <span className="text-neutral-400">Başarı Oranı: </span>
                    <span className="font-semibold text-neutral-800">{formatRate(topRecommendation.successRate)}</span>
                  </div>
                  <div>
                    <span className="text-neutral-400">Soru Sayısı: </span>
                    <span className="font-semibold text-neutral-800">{topRecommendation.totalQuestions}</span>
                  </div>
                  <div>
                    <span className="text-neutral-400">Öncelik Tipi: </span>
                    <span className="font-semibold text-neutral-800">{getRecommendationTypeLabel(topRecommendation.recommendationType)}</span>
                  </div>
                </div>
              </div>
            ) : (
              <p className="text-xs text-neutral-500 py-3">
                Kayıtlı sınav sonuçlarınıza göre öncelikli kritik eksik bulunmuyor.
              </p>
            )}
          </div>

          <div className="pt-2 border-t border-neutral-100 flex justify-end">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onNavigateTab('priorities')}
              className="text-xs"
            >
              Tüm Öncelikleri İncele →
            </Button>
          </div>
        </Card>

        {/* Study Plan Proposal Hero Card */}
        <Card className="p-4 sm:p-5 bg-white border border-neutral-100 flex flex-col justify-between space-y-4">
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <span className="inline-flex items-center gap-1.5 text-xs font-semibold text-indigo-700 bg-indigo-50 px-2.5 py-1 rounded-full border border-indigo-100">
                <Calendar className="h-3.5 w-3.5 text-indigo-600" />
                Önerilen Çalışma Planı (Teklif)
              </span>
              <span className="text-xs font-medium text-neutral-400">
                {studyPlan ? `${studyPlan.planDays} Günlük Teklif` : ''}
              </span>
            </div>

            {studyPlan ? (
              <div className="space-y-3 pt-1">
                <div className="flex items-baseline justify-between">
                  <div>
                    <p className="text-2xl font-bold text-neutral-900">
                      {studyPlan.totalPlannedMinutes}{' '}
                      <span className="text-xs font-normal text-neutral-500">
                        / {studyPlan.totalAvailableMinutes} dk planlandı
                      </span>
                    </p>
                    <p className="text-xs text-neutral-500 mt-0.5">
                      Günlük ortalama {studyPlan.dailyStudyMinutes} dk kapasiteden dağıtıldı
                    </p>
                  </div>
                  <Badge variant="info">
                    %{Math.round(studyPlan.utilizationRate * 100)} Kullanım
                  </Badge>
                </div>

                <div className="grid grid-cols-2 gap-2 text-xs bg-neutral-50 p-2.5 rounded-xl border border-neutral-100">
                  <div>
                    <span className="text-neutral-500 block">Planlanan Oturum:</span>
                    <span className="font-semibold text-neutral-800">
                      {studyPlan.days.reduce((acc, d) => acc + d.sessionCount, 0)} Oturum ({studyPlan.sessionMinutes} dk/oturum)
                    </span>
                  </div>
                  <div>
                    <span className="text-neutral-500 block">Planlanan Konular:</span>
                    <span className="font-semibold text-neutral-800">
                      {studyPlan.scheduledRecommendationCount} Konu Hedefi
                    </span>
                  </div>
                </div>

                {studyPlan.unallocatedMinutes > 0 && (
                  <p className="text-xs text-neutral-500 italic">
                    Plan dışında kalan esnek süre: {studyPlan.unallocatedMinutes} dk
                  </p>
                )}
              </div>
            ) : (
              <p className="text-xs text-neutral-500 py-3">
                Çalışma planı teklifi oluşturuluyor...
              </p>
            )}
          </div>

          <div className="pt-2 border-t border-neutral-100 flex justify-end">
            <Button
              variant="outline"
              size="sm"
              onClick={() => onNavigateTab('study-plan')}
              className="text-xs"
            >
              Haftalık Planı İncele & Özelleştir →
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
};
