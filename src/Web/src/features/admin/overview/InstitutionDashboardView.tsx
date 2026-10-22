import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialAdminOverviewData } from './mockData';
import { InstitutionOverviewViewModel, Timeframe } from './types';
import { KpiGrid } from './components/KpiGrid';
import { AcademicTrendChart } from './components/AcademicTrendChart';
import { AttentionSummaryCard } from './components/AttentionSummaryCard';
import { CoachPortfolioCard } from './components/CoachPortfolioCard';
import { adminService } from '@/services/adminService';

const TIMEFRAME_LABELS: Record<Timeframe, string> = {
  '1m': 'Son 4 Deneme',
  '3m': 'Son 3 Ay',
  '6m': 'Son 6 Ay',
};

export interface InstitutionDashboardViewProps {
  initialData?: InstitutionOverviewViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const InstitutionDashboardView: React.FC<InstitutionDashboardViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [data, setData] = useState<InstitutionOverviewViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);
  const [timeframe, setTimeframe] = useState<Timeframe>('1m');

  const fetchOverview = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const [studentsRes, coachesRes] = await Promise.all([
        adminService.listStudents({ page: 1, pageSize: 1 }),
        adminService.listCoaches({ page: 1, pageSize: 1 }),
      ]);

      const totalStudents = studentsRes?.totalCount ?? 0;
      const totalCoaches = coachesRes?.totalCount ?? 0;

      const updatedKpis = initialAdminOverviewData.kpis.map((kpi) => {
        if (kpi.label.includes('Öğrenci')) {
          return { ...kpi, value: totalStudents.toString() };
        }
        if (kpi.label.includes('Koç') || kpi.label.includes('Portföy')) {
          return { ...kpi, value: `${totalCoaches} Koç` };
        }
        return kpi;
      });

      setData({
        ...initialAdminOverviewData,
        kpis: updatedKpis,
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Kurum gösterge paneli yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchOverview();
    }
  }, [initialData, fetchOverview]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchOverview();
    }
  };

  const activeData = data || initialData || initialAdminOverviewData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const chartData = activeData.trendData[timeframe] || activeData.trendData['1m'];

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none" data-testid="admin-dashboard-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
          <Skeleton className="h-28 w-full rounded-2xl" />
          <Skeleton className="h-28 w-full rounded-2xl" />
          <Skeleton className="h-28 w-full rounded-2xl" />
          <Skeleton className="h-28 w-full rounded-2xl" />
        </div>
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
          <Skeleton className="h-72 xl:col-span-2 rounded-2xl" />
          <Skeleton className="h-72 rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none" data-testid="admin-dashboard-error">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Veriler Yüklenemedi"
        >
          {activeError}
        </Alert>
        <div className="text-center pt-2">
          <Button
            variant="primary"
            size="sm"
            onClick={handleRetry}
            leftIcon={<RefreshCw className="h-4 w-4" />}
          >
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="admin-dashboard-view">
      {/* 1. Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Kurumsal Genel Bakış
          </h1>
          <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
            {activeData.institutionName} · {activeData.branchCount} Şube
          </p>
        </div>

        {/* Timeframe Selector */}
        <div className="flex items-center gap-1 bg-neutral-100 p-1 rounded-xl self-start sm:self-auto border border-neutral-200">
          {(['1m', '3m', '6m'] as Timeframe[]).map((tf) => (
            <button
              key={tf}
              type="button"
              onClick={() => setTimeframe(tf)}
              className={cn(
                'px-3 py-1.5 text-xs font-semibold rounded-lg transition-colors',
                timeframe === tf
                  ? 'bg-white text-navy-900 shadow-soft-xs'
                  : 'text-neutral-500 hover:text-neutral-800'
              )}
            >
              {TIMEFRAME_LABELS[tf]}
            </button>
          ))}
        </div>
      </div>

      {/* 2. Top KPI Grid */}
      <KpiGrid kpis={activeData.kpis} />

      {/* 3. Middle Section: TYT Trend Chart + Attention Summary */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        <div className="xl:col-span-2">
          <AcademicTrendChart
            data={chartData}
            timeframeLabel={TIMEFRAME_LABELS[timeframe]}
          />
        </div>
        <div>
          <AttentionSummaryCard stats={activeData.attentionStats} />
        </div>
      </div>

      {/* 4. Bottom Section: Coach Portfolio */}
      <CoachPortfolioCard
        coaches={activeData.coaches}
        timeframeLabel={TIMEFRAME_LABELS[timeframe]}
        onSelectCoach={(coachId) => navigate(`/admin/coaches?coachId=${coachId}`)}
      />
    </div>
  );
};
