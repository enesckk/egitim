import React from 'react';
import { BarChart3, AlertCircle, RefreshCw, Building2 } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialAdminReportsData } from './mockData';
import { AdminReportsViewModel } from './types';

export interface AdminReportsViewProps {
  initialData?: AdminReportsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminReportsView: React.FC<AdminReportsViewProps> = ({
  initialData = initialAdminReportsData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-64 w-full rounded-2xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
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
          title="Raporlar Yüklenemedi"
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* 1. Header */}
      <div>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Kurum Raporları & Analiz
        </h1>
        <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
          Şubeler arası performans, plan uyumu dağılımı ve akademik analizler
        </p>
      </div>

      {/* 2. Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
        {/* A. Plan Adherence Distribution */}
        <div className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm space-y-4">
          <div className="flex items-center gap-2 pb-2 border-b border-neutral-100">
            <BarChart3 className="h-4 w-4 text-primary-600" />
            <h2 className="font-semibold text-neutral-800 text-sm">
              Öğrenci Plan Uyumu Dağılımı
            </h2>
          </div>

          <div className="space-y-4">
            {initialData.adherenceDistribution.map((item) => (
              <div key={item.range} className="space-y-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-neutral-700">{item.range}</span>
                  <span className="font-mono text-neutral-900 font-bold">
                    {item.studentCount} Öğrenci (%{item.percentage})
                  </span>
                </div>
                <div className="h-2.5 w-full bg-neutral-100 rounded-full overflow-hidden">
                  <div
                    className={cn('h-full rounded-full transition-all duration-500', item.color)}
                    style={{ width: `${item.percentage}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* B. Branch Comparison Table */}
        <div className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm space-y-4">
          <div className="flex items-center gap-2 pb-2 border-b border-neutral-100">
            <Building2 className="h-4 w-4 text-primary-600" />
            <h2 className="font-semibold text-neutral-800 text-sm">
              Şubeler Arası Başarı Karşılaştırması
            </h2>
          </div>

          <div className="divide-y divide-neutral-50">
            {initialData.branchComparisons.map((branch) => (
              <div
                key={branch.branchName}
                className="py-3 flex items-center justify-between gap-2"
              >
                <div>
                  <h4 className="text-sm font-semibold text-neutral-900">
                    {branch.branchName}
                  </h4>
                  <p className="text-xs text-neutral-400 mt-0.5">
                    {branch.studentCount} Öğrenci · {branch.coachCount} Koç
                  </p>
                </div>
                <div className="text-right font-mono text-xs space-y-0.5">
                  <span className="block font-bold text-primary-700">
                    {branch.averageNet} Net Ort.
                  </span>
                  <span className="text-success font-semibold">
                    %{branch.averageAdherence} Uyum
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
};
