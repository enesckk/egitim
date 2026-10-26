import React from 'react';
import {
  ChevronRight,
  Target,
  ArrowRight,
  CalendarDays,
} from 'lucide-react';
import {
  StudentKnowledgeMapDto,
  AcademicRecommendationDto,
  AcademicStudyPlanDto,
  StudentSubjectPerformanceDto,
  SubjectTrendDto,
  SubjectClassificationDto,
  StudentQuestionStyleAnalyticsDto,
} from '../types';
import { AcademicAiSummaryCard } from './AcademicAiSummaryCard';
import { QuestionStyleAnalyticsSection } from './QuestionStyleAnalyticsSection';
import { AcademicPerformanceCard } from './AcademicPerformanceCard';

interface AcademicOverviewTabProps {
  studentId?: string;
  knowledgeMap: StudentKnowledgeMapDto | null;
  recommendations: AcademicRecommendationDto[];
  studyPlan: AcademicStudyPlanDto | null;
  subjects: StudentSubjectPerformanceDto[];
  trends: SubjectTrendDto[];
  classifications: SubjectClassificationDto[];
  questionStyles: StudentQuestionStyleAnalyticsDto[];
  onNavigateTab: (tab: 'performance' | 'knowledge-map' | 'priorities' | 'study-plan') => void;
}

export const AcademicOverviewTab: React.FC<AcademicOverviewTabProps> = ({
  studentId,
  knowledgeMap,
  recommendations,
  studyPlan,
  subjects,
  trends,
  classifications,
  questionStyles,
  onNavigateTab,
}) => {
  const summary = knowledgeMap?.summary;
  const topRecommendation = recommendations.length > 0 ? recommendations[0] : null;

  // Real subject status counts
  const totalEvaluated = summary?.subjectsWithEvidence ?? (summary?.totalSubjects ?? subjects.length);
  const strongCount = summary?.strongSubjects ?? 0;
  const weakCount = summary?.weakSubjects ?? 0;
  const criticalCount = summary?.criticalSubjects ?? 0;
  const developingCount = Math.max(
    0,
    totalEvaluated - (strongCount + weakCount + criticalCount)
  );

  return (
    <div className="space-y-4 sm:space-y-5">
      {/* 1. TOP ROW: Genel Durumum & En Öncelikli Çalışma Alanım */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-4 sm:gap-5">
        {/* Left Column (lg:col-span-7): Genel Durumum & AI Summary */}
        <div className="lg:col-span-7 space-y-4">
          {/* Genel Durumum Card */}
          <div className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 flex flex-col justify-between">
            <div className="flex items-center justify-between mb-3">
              <div>
                <h3 className="text-sm font-bold text-[#14233E]">Genel Durumum</h3>
                <span className="text-[11px] text-neutral-400">Değerlendirilen ders başarı dağılımı</span>
              </div>
              <button
                type="button"
                onClick={() => onNavigateTab('knowledge-map')}
                className="text-xs font-semibold text-[#315FE8] hover:underline inline-flex items-center gap-0.5"
              >
                Müfredat Detayı <ChevronRight className="w-3.5 h-3.5" />
              </button>
            </div>

            <div className="grid grid-cols-4 gap-2 text-center">
              <div className="bg-blue-50/70 border border-blue-100 rounded-xl py-3 px-1">
                <span className="text-xl sm:text-2xl font-bold text-[#14233E] block leading-none">
                  {totalEvaluated}
                </span>
                <span className="text-[11px] font-medium text-neutral-500 mt-1 block">
                  Ders
                </span>
              </div>

              <div className="bg-emerald-50/70 border border-emerald-100 rounded-xl py-3 px-1">
                <span className="text-xl sm:text-2xl font-bold text-emerald-700 block leading-none">
                  {strongCount}
                </span>
                <span className="text-[11px] font-medium text-emerald-800 mt-1 block">
                  Güçlü
                </span>
              </div>

              <div className="bg-amber-50/70 border border-amber-100 rounded-xl py-3 px-1">
                <span className="text-xl sm:text-2xl font-bold text-amber-700 block leading-none">
                  {developingCount}
                </span>
                <span className="text-[11px] font-medium text-amber-800 mt-1 block">
                  Gelişiyor
                </span>
              </div>

              <div className="bg-rose-50/70 border border-rose-100 rounded-xl py-3 px-1">
                <span className="text-xl sm:text-2xl font-bold text-rose-700 block leading-none">
                  {weakCount + criticalCount}
                </span>
                <span className="text-[11px] font-medium text-rose-800 mt-1 block">
                  Çalışmalı
                </span>
              </div>
            </div>
          </div>

          {/* Akademik Değerlendirmem (AI Summary / Rule-based) */}
          {studentId && (
            <AcademicAiSummaryCard
              studentId={studentId}
              audience="student"
              onNavigateDetails={() => onNavigateTab('priorities')}
            />
          )}
        </div>

        {/* Right Column (lg:col-span-5): 3. PRIORITY FOCUS (Navy Card) */}
        <div className="lg:col-span-5 flex flex-col">
          {topRecommendation ? (
            <div className="bg-[#14233E] text-white rounded-2xl p-5 sm:p-6 shadow-sm flex flex-col justify-between flex-1 relative overflow-hidden">
              {/* Subtle background glow */}
              <div className="absolute -top-12 -right-12 w-36 h-36 bg-[#315FE8]/20 rounded-full blur-2xl pointer-events-none" />

              <div className="relative z-10">
                <div className="flex items-center gap-2 mb-2">
                  <div className="w-8 h-8 rounded-lg bg-white/10 flex items-center justify-center text-white">
                    <Target className="w-4 h-4 text-blue-300" />
                  </div>
                  <span className="text-xs font-semibold text-blue-200">
                    En Öncelikli Çalışma Alanım
                  </span>
                </div>

                <div className="mt-3">
                  <div className="flex items-center gap-2">
                    <h4 className="text-base sm:text-lg font-bold text-white tracking-tight">
                      {topRecommendation.subjectName || 'Matematik'} — {topRecommendation.name}
                    </h4>
                    <span className="text-[10px] font-bold text-rose-300 bg-rose-500/20 px-2 py-0.5 rounded-md border border-rose-400/30">
                      Öncelikli
                    </span>
                  </div>

                  <p className="text-xs text-blue-100/80 mt-2 leading-relaxed">
                    Deneme sonuçlarına göre bu konuda daha fazla çalışmaya ve konu tekrarına ihtiyaç var.
                  </p>
                </div>
              </div>

              <div className="relative z-10 mt-6 pt-4 border-t border-white/10 flex items-center justify-between">
                <button
                  type="button"
                  onClick={() => onNavigateTab('priorities')}
                  className="inline-flex items-center gap-2 px-5 py-2.5 rounded-full bg-[#315FE8] hover:bg-[#254fc9] active:scale-95 text-white text-xs font-semibold shadow-sm transition-all"
                >
                  Çalışma önerisini incele
                  <ArrowRight className="w-3.5 h-3.5" />
                </button>
              </div>
            </div>
          ) : (
            <div className="bg-[#14233E] text-white rounded-2xl p-5 sm:p-6 shadow-sm flex flex-col justify-between flex-1">
              <div>
                <span className="text-xs font-semibold text-blue-200">
                  En Öncelikli Çalışma Alanım
                </span>
                <h4 className="text-base font-bold text-white mt-2">
                  Tüm Alanlarda Dengeli Başarım
                </h4>
                <p className="text-xs text-blue-100/80 mt-1">
                  Son sınav verilerine göre kritik eksik bulunmuyor. Genel soru çözümü ile devam edebilirsin.
                </p>
              </div>
              <div className="mt-4">
                <button
                  type="button"
                  onClick={() => onNavigateTab('priorities')}
                  className="px-4 py-2 rounded-full bg-white/15 hover:bg-white/20 text-white text-xs font-semibold transition-all"
                >
                  Tüm Konuları Gör
                </button>
              </div>
            </div>
          )}
        </div>
      </div>

      {/* 2. MIDDLE ROW: Performans & Gelişim Chart and Subject Breakdown */}
      <AcademicPerformanceCard
        subjects={subjects}
        trends={trends}
        classifications={classifications}
        onNavigateDetail={() => onNavigateTab('performance')}
      />

      {/* 3. BOTTOM ROW: Soru Tarzı Analizi & Çalışma Önerim */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-5">
        {/* Soru Tarzı Analizim */}
        <QuestionStyleAnalyticsSection
          styles={questionStyles}
          onNavigateDetail={() => onNavigateTab('knowledge-map')}
        />

        {/* Çalışma Önerim / Haftalık Plan Teklifi */}
        <div className="bg-white rounded-2xl p-5 shadow-xs border border-neutral-200/70 flex flex-col justify-between">
          <div>
            <div className="flex items-center justify-between mb-2">
              <div className="flex items-center gap-2">
                <div className="w-8 h-8 rounded-lg bg-blue-50 text-[#315FE8] flex items-center justify-center">
                  <CalendarDays className="w-4 h-4" />
                </div>
                <div>
                  <h3 className="text-sm font-bold text-[#14233E]">Çalışma Önerim</h3>
                </div>
              </div>
              <span className="text-[10px] font-bold text-blue-600 bg-blue-50 px-2 py-0.5 rounded-md">
                {studyPlan?.planDays || 7} Günlük Taslak Plan
              </span>
            </div>

            <p className="text-xs text-neutral-500 mt-2 leading-relaxed">
              Eksiklerine göre hazırlanan haftalık çalışma planını inceleyebilir ve hedeflerine ekleyebilirsin.
            </p>

            <div className="grid grid-cols-2 gap-2 mt-4 bg-neutral-50/70 p-3 rounded-xl border border-neutral-100 text-xs">
              <div>
                <span className="text-[11px] text-neutral-400 block">Planlanan Oturum:</span>
                <span className="font-bold text-[#14233E] mt-0.5 block">
                  {studyPlan?.days.reduce((acc, d) => acc + d.sessionCount, 0) || 28} Oturum
                </span>
                <span className="text-[10px] text-neutral-400">
                  ({studyPlan?.sessionMinutes || 30} dk/oturum)
                </span>
              </div>
              <div>
                <span className="text-[11px] text-neutral-400 block">Konu Hedefi:</span>
                <span className="font-bold text-[#14233E] mt-0.5 block">
                  {studyPlan?.scheduledRecommendationCount || 7} Konu Hedefi
                </span>
                <span className="text-[10px] text-neutral-400">Öncelik sıralı</span>
              </div>
            </div>
          </div>

          <div className="mt-5 pt-3 border-t border-neutral-100 flex items-center justify-between">
            <span className="text-[11px] text-neutral-400">Özelleştirilebilir taslak plan</span>
            <button
              type="button"
              onClick={() => onNavigateTab('study-plan')}
              className="text-xs font-semibold text-[#315FE8] hover:underline inline-flex items-center gap-1"
            >
              Planı İncele ve Özelleştir <ArrowRight className="w-3.5 h-3.5" />
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
