import React, { useState, useEffect, useCallback } from 'react';
import {
  LayoutDashboard,
  BarChart3,
  FolderTree,
  Sparkles,
  Calendar,
  AlertCircle,
  RefreshCw,
  Filter,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { useAuth } from '@/auth';
import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import { Skeleton } from '@/components/ui/Skeleton';
import { Select } from '@/components/ui/Select';
import { academicService, AcademicQueryParams } from '@/services/academicService';
import {
  StudentSubjectPerformanceDto,
  SubjectTrendDto,
  SubjectClassificationDto,
  StudentKnowledgeMapDto,
  AcademicRecommendationDto,
  AcademicStudyPlanDto,
} from './types';
import { AcademicOverviewTab } from './components/AcademicOverviewTab';
import { AcademicPerformanceTab } from './components/AcademicPerformanceTab';
import { AcademicKnowledgeMapTab } from './components/AcademicKnowledgeMapTab';
import { AcademicPrioritiesTab } from './components/AcademicPrioritiesTab';
import { AcademicStudyPlanTab } from './components/AcademicStudyPlanTab';

export type AcademicTabKey = 'overview' | 'performance' | 'knowledge-map' | 'priorities' | 'study-plan';

export interface StudentAcademicViewProps {
  studentIdOverride?: string;
}

export const StudentAcademicView: React.FC<StudentAcademicViewProps> = ({
  studentIdOverride,
}) => {
  const { user } = useAuth();
  const [resolvedStudentId, setResolvedStudentId] = useState<string | null>(studentIdOverride || null);
  const [isResolvingStudentId, setIsResolvingStudentId] = useState<boolean>(!studentIdOverride);
  const [studentLinkError, setStudentLinkError] = useState<string | null>(null);

  const [activeTab, setActiveTab] = useState<AcademicTabKey>('overview');
  const [examTypeId, setExamTypeId] = useState<string>('');
  const [planDays, setPlanDays] = useState<number>(7);
  const [dailyStudyMinutes, setDailyStudyMinutes] = useState<number>(120);

  // Data states
  const [subjects, setSubjects] = useState<StudentSubjectPerformanceDto[]>([]);
  const [trends, setTrends] = useState<SubjectTrendDto[]>([]);
  const [classifications, setClassifications] = useState<SubjectClassificationDto[]>([]);
  const [knowledgeMap, setKnowledgeMap] = useState<StudentKnowledgeMapDto | null>(null);
  const [recommendations, setRecommendations] = useState<AcademicRecommendationDto[]>([]);
  const [studyPlan, setStudyPlan] = useState<AcademicStudyPlanDto | null>(null);

  // Status states
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Request sequence ref for race condition safety
  const requestSeqRef = React.useRef<number>(0);

  // Step 1: Resolve actual Student.Id entity ID for current authenticated user if not overridden
  useEffect(() => {
    let isMounted = true;
    if (studentIdOverride) {
      setResolvedStudentId(studentIdOverride);
      setIsResolvingStudentId(false);
      return;
    }

    const resolveStudentId = async () => {
      setIsResolvingStudentId(true);
      setStudentLinkError(null);

      const sid = await academicService.getCurrentStudentId();
      if (isMounted) {
        if (sid) {
          setResolvedStudentId(sid);
        } else {
          setStudentLinkError('Öğrenci profili ile hesap eşleştirmesi bulunamadı.');
        }
        setIsResolvingStudentId(false);
      }
    };

    resolveStudentId();
    return () => {
      isMounted = false;
    };
  }, [studentIdOverride, user?.id]);

  // Step 2: Fetch Academic Intelligence data with AbortController and sequence protection
  const fetchAcademicData = useCallback(
    async (signal?: AbortSignal, sequenceId?: number) => {
      if (!resolvedStudentId) return;

      setIsLoading(true);
      setErrorMessage(null);

      try {
        const params: AcademicQueryParams = {
          examTypeId: examTypeId || undefined,
          planDays,
          dailyStudyMinutes,
          limit: 50,
          recommendationLimit: 10,
        };

        const [
          subjectsRes,
          trendsRes,
          classifRes,
          mapRes,
          recRes,
          planRes,
        ] = await Promise.all([
          academicService.getSubjectPerformance(resolvedStudentId, params, signal),
          academicService.getSubjectTrends(resolvedStudentId, params, signal),
          academicService.getSubjectClassifications(resolvedStudentId, params, signal),
          academicService.getKnowledgeMap(resolvedStudentId, params, signal),
          academicService.getRecommendations(resolvedStudentId, params, signal),
          academicService.getStudyPlan(resolvedStudentId, params, signal),
        ]);

        // Race protection check: drop response if a newer request was dispatched
        if (sequenceId !== undefined && sequenceId !== requestSeqRef.current) {
          return;
        }

        setSubjects(subjectsRes);
        setTrends(trendsRes);
        setClassifications(classifRes);
        setKnowledgeMap(mapRes);
        setRecommendations(recRes);
        setStudyPlan(planRes);
      } catch (err: unknown) {
        if (err instanceof Error && err.name === 'AbortError') {
          return; // Ignore aborted request errors
        }
        if (sequenceId !== undefined && sequenceId !== requestSeqRef.current) {
          return;
        }
        console.error('Failed to load academic intelligence data:', err);
        const msg = err instanceof Error ? err.message : 'Akademik zeka verileri yüklenirken bir hata oluştu.';
        setErrorMessage(msg);
      } finally {
        if (sequenceId === undefined || sequenceId === requestSeqRef.current) {
          setIsLoading(false);
        }
      }
    },
    [resolvedStudentId, examTypeId, planDays, dailyStudyMinutes]
  );

  useEffect(() => {
    if (!resolvedStudentId) return;

    const controller = new AbortController();
    const seq = ++requestSeqRef.current;

    fetchAcademicData(controller.signal, seq);

    return () => {
      controller.abort();
    };
  }, [resolvedStudentId, fetchAcademicData]);

  // Loading Student Context State
  if (isResolvingStudentId) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="student-academic-resolving">
        <Skeleton className="h-10 w-64 rounded-xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Student Account Unlinked Error State
  if (studentLinkError) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4" data-testid="student-academic-unlinked-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Hesap Eşleştirmesi Eksik">
          {studentLinkError}
        </Alert>
      </div>
    );
  }

  // Academic Intelligence Loading State Skeleton
  if (isLoading && !knowledgeMap) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="student-academic-loading">
        <Skeleton className="h-10 w-64 rounded-xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="grid grid-cols-2 lg:grid-cols-5 gap-3">
          <Skeleton className="h-24 rounded-xl" />
          <Skeleton className="h-24 rounded-xl" />
          <Skeleton className="h-24 rounded-xl" />
          <Skeleton className="h-24 rounded-xl" />
          <Skeleton className="h-24 rounded-xl" />
        </div>
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State Alert
  if (errorMessage && !knowledgeMap) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4" data-testid="student-academic-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Akademik Zeka Yüklenemedi">
          {errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={() => fetchAcademicData()} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const navTabs = [
    { key: 'overview', label: 'Genel Bakış', icon: <LayoutDashboard className="h-4 w-4" /> },
    { key: 'performance', label: 'Performans & Trendler', icon: <BarChart3 className="h-4 w-4" /> },
    { key: 'knowledge-map', label: 'Müfredat Haritası', icon: <FolderTree className="h-4 w-4" /> },
    { key: 'priorities', label: 'Akademik Öncelikler', icon: <Sparkles className="h-4 w-4" /> },
    { key: 'study-plan', label: 'Çalışma Planı Önerisi', icon: <Calendar className="h-4 w-4" /> },
  ] as const;

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="student-academic-view">
      {/* 1. Header & Exam Filter */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-100 pb-3 sm:pb-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Akademik Zeka & Analiz
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Gerçek sınav verilerine dayalı başarım, gelişim trendi ve çalışma planı teklifi
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Filter className="h-4 w-4 text-neutral-400" />
          <Select
            options={[
              { value: '', label: 'Tüm Sınav Türleri' },
              { value: 'TYT', label: 'TYT' },
              { value: 'AYT', label: 'AYT' },
              { value: 'LGS', label: 'LGS' },
            ]}
            value={examTypeId}
            onChange={(e) => setExamTypeId(e.target.value)}
            placeholder="Sınav Türü Filtrele"
            className="text-xs min-h-[34px] py-1"
          />
        </div>
      </div>

      {/* 2. Navigation Tabs */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-1.5 flex items-center gap-1 overflow-x-auto no-scrollbar shadow-soft-sm">
        {navTabs.map((tab) => {
          const isActive = activeTab === tab.key;
          return (
            <button
              key={tab.key}
              type="button"
              onClick={() => setActiveTab(tab.key as AcademicTabKey)}
              className={cn(
                'flex items-center gap-2 px-3.5 py-2 rounded-xl text-xs font-medium transition-all shrink-0 min-h-[36px]',
                isActive
                  ? 'bg-primary-600 text-white shadow-sm font-semibold'
                  : 'text-neutral-600 hover:text-neutral-900 hover:bg-neutral-50'
              )}
            >
              {tab.icon}
              <span>{tab.label}</span>
            </button>
          );
        })}
      </div>

      {/* 3. Active Tab Content */}
      <div className="pt-1">
        {activeTab === 'overview' && (
          <AcademicOverviewTab
            studentId={resolvedStudentId || undefined}
            knowledgeMap={knowledgeMap}
            recommendations={recommendations}
            studyPlan={studyPlan}
            onNavigateTab={(tabKey) => setActiveTab(tabKey)}
          />
        )}

        {activeTab === 'performance' && (
          <AcademicPerformanceTab
            subjects={subjects}
            trends={trends}
            classifications={classifications}
          />
        )}

        {activeTab === 'knowledge-map' && (
          <AcademicKnowledgeMapTab knowledgeMap={knowledgeMap} />
        )}

        {activeTab === 'priorities' && (
          <AcademicPrioritiesTab
            recommendations={recommendations}
            onNavigateToStudyPlan={() => setActiveTab('study-plan')}
          />
        )}

        {activeTab === 'study-plan' && (
          <AcademicStudyPlanTab
            studentId={resolvedStudentId}
            studyPlan={studyPlan}
            planDays={planDays}
            dailyStudyMinutes={dailyStudyMinutes}
            onChangePlanDays={setPlanDays}
            onChangeDailyStudyMinutes={setDailyStudyMinutes}
            onRefreshPlan={fetchAcademicData}
            isLoading={isLoading}
          />
        )}
      </div>
    </div>
  );
};
