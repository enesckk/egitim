import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertCircle, RefreshCw, Building2 } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { InstitutionOverviewViewModel } from './types';
import { KpiGrid } from './components/KpiGrid';
import { AcademicTrendChart } from './components/AcademicTrendChart';
import { AttentionSummaryCard } from './components/AttentionSummaryCard';
import { CoachPortfolioCard } from './components/CoachPortfolioCard';
import { adminService } from '@/services/adminService';

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

  const fetchOverview = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const overview = await adminService.getInstitutionOverview();
      setData(overview);
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

  const activeData = data || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Real trend points from server response
  const chartData = activeData?.trendData
    ? (activeData.trendData['1m'] || activeData.trendData['3m'] || activeData.trendData['6m'] || [])
    : [];

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="admin-dashboard-loading">
        <Skeleton className="h-12 w-1/3 rounded-2xl" />
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
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

  // Error State or Empty
  if (activeError || !activeData) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none p-4" data-testid="admin-dashboard-error">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Veriler Yüklenemedi"
        >
          {activeError || 'Kurum genel bakış verisi bulunamadı.'}
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

  // Normalize KPI subtitles to avoid misleading "şubede" text for classroom counts
  const correctedKpis = (activeData.kpis || []).map((kpi) => {
    if (kpi.iconType === 'users' && kpi.sub.includes('şubede')) {
      const classCount = activeData.branchCount;
      return {
        ...kpi,
        sub: classCount > 0 ? `${classCount} aktif sınıfta` : 'Kayıtlı öğrenciler',
      };
    }
    return kpi;
  });

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="admin-dashboard-view">
      {/* 1. Header Banner */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-200/70 pb-4">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-navy-950 tracking-tight leading-tight">
              Kurumsal Genel Bakış
            </h1>
            <span className="text-xs font-bold text-navy-800 bg-neutral-100 border border-neutral-200 px-2.5 py-0.5 rounded-full">
              Yönetici Paneli
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1 flex items-center gap-1.5">
            <Building2 className="h-3.5 w-3.5 text-neutral-400" />
            <span className="font-semibold text-neutral-700">{activeData.institutionName}</span>
            <span>·</span>
            <span>{activeData.branchCount} Aktif Sınıf</span>
          </p>
        </div>
      </div>

      {/* 2. Top KPI Grid with Real Corrected Semantics */}
      <KpiGrid kpis={correctedKpis} />

      {/* 3. Middle Section: Real TYT Trend Chart + Attention Summary */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        <div className="xl:col-span-2">
          <AcademicTrendChart data={chartData} />
        </div>
        <div>
          <AttentionSummaryCard stats={activeData.attentionStats} />
        </div>
      </div>

      {/* 4. Bottom Section: Coach Portfolio */}
      <CoachPortfolioCard
        coaches={activeData.coaches}
        onSelectCoach={(coachId) => navigate(`/admin/coaches?coachId=${coachId}`)}
      />
    </div>
  );
};

export default InstitutionDashboardView;
