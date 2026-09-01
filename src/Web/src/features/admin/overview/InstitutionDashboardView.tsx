import React, { useState } from 'react';
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
  initialData = initialAdminOverviewData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [timeframe, setTimeframe] = useState<Timeframe>('1m');

  const chartData = initialData.trendData[timeframe] || initialData.trendData['1m'];

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
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
  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Kurum Verileri Yüklenemedi"
        >
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button
              variant="primary"
              size="sm"
              onClick={onRetry}
              leftIcon={<RefreshCw className="h-4 w-4" />}
            >
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto select-none space-y-5 sm:space-y-6">
      {/* 1. Header & Timeframe Selector */}
      <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Genel Bakış
          </h1>
          <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
            {initialData.institutionName} · {initialData.branchCount} Şube
          </p>
        </div>

        {/* Timeframe selector */}
        <div className="flex items-center bg-white border border-neutral-200 rounded-xl p-1 shadow-soft-xs self-start">
          {(Object.entries(TIMEFRAME_LABELS) as [Timeframe, string][]).map(([key, label]) => (
            <button
              key={key}
              type="button"
              onClick={() => setTimeframe(key)}
              className={cn(
                'px-3.5 py-1.5 rounded-lg text-xs sm:text-sm font-medium transition-colors select-none',
                timeframe === key
                  ? 'bg-navy-900 text-white font-semibold'
                  : 'text-neutral-500 hover:text-neutral-800'
              )}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      {/* 2. Top KPI Grid */}
      <KpiGrid kpis={initialData.kpis} />

      {/* 3. Middle Section: TYT Trend Chart + Attention Summary */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
        <div className="xl:col-span-2">
          <AcademicTrendChart
            data={chartData}
            timeframeLabel={TIMEFRAME_LABELS[timeframe]}
          />
        </div>
        <div>
          <AttentionSummaryCard stats={initialData.attentionStats} />
        </div>
      </div>

      {/* 4. Bottom Section: Coach Portfolio */}
      <CoachPortfolioCard
        coaches={initialData.coaches}
        timeframeLabel={TIMEFRAME_LABELS[timeframe]}
        onSelectCoach={(coachId) => navigate(`/admin/coaches?coachId=${coachId}`)}
      />
    </div>
  );
};
