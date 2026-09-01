import React from 'react';
import { BarChart3, AlertTriangle, AlertCircle, RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialCoachReportsData } from './mockData';
import { CoachReportsViewModel } from './types';

export interface CoachReportsViewProps {
  initialData?: CoachReportsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const CoachReportsView: React.FC<CoachReportsViewProps> = ({
  initialData = initialCoachReportsData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
        </div>
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Raporlar Yüklenemedi">
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button variant="primary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* 1. Header */}
      <div>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Portföy Gelişim Raporu
        </h1>
        <p className="text-xs text-neutral-400 mt-0.5">
          Atanmış {initialData.totalAssignedStudents} öğrencinin plan uyumu ve akademik çalışma istatistikleri
        </p>
      </div>

      {/* 2. Top Summary KPI Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 sm:gap-3.5">
        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <span className="text-xs text-neutral-400">Plan Uyumu Ortalaması</span>
          <p className="font-mono text-xl sm:text-2xl font-bold text-success mt-1">
            %{initialData.portfolioOverallAdherence}
          </p>
          <span className="text-[10px] text-neutral-400">Tüm sınıflar geneli</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <span className="text-xs text-neutral-400">Haftalık Çalışma Süresi</span>
          <p className="font-mono text-xl sm:text-2xl font-bold text-neutral-900 mt-1">
            {initialData.portfolioAverageStudyHours} sa
          </p>
          <span className="text-[10px] text-neutral-400">Öğrenci başına ortalama</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <span className="text-xs text-neutral-400">Toplam Öğrenci</span>
          <p className="font-mono text-xl sm:text-2xl font-bold text-primary-700 mt-1">
            {initialData.totalAssignedStudents}
          </p>
          <span className="text-[10px] text-neutral-400">Aktif koçluk portföyü</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <span className="text-xs text-neutral-400">Riskli / Dikkat Gerektiren</span>
          <p className="font-mono text-xl sm:text-2xl font-bold text-danger mt-1">
            {initialData.atRiskStudentsCount} Öğrenci
          </p>
          <span className="text-[10px] text-danger font-medium">Acil takipte</span>
        </div>
      </div>

      {/* 3. Grade-level Adherence Breakdown */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm space-y-4">
        <div className="flex items-center gap-2">
          <BarChart3 className="h-4 w-4 text-primary-600" />
          <h3 className="text-sm sm:text-base font-semibold text-neutral-900">
            Sınıf Bazında Çalışma & Plan Uyumu
          </h3>
        </div>

        <div className="space-y-3.5">
          {initialData.gradeBreakdown.map((item) => (
            <div key={item.grade} className="space-y-1.5">
              <div className="flex flex-col sm:flex-row sm:items-center justify-between text-xs gap-1">
                <span className="font-semibold text-neutral-800">{item.grade}</span>
                <div className="flex items-center gap-3 text-neutral-500">
                  <span>{item.studentCount} Öğrenci</span>
                  <span>Ort. {item.averageHours} saat/hf</span>
                  <span className="font-mono font-bold text-neutral-900">
                    %{item.adherencePercentage} Uyum
                  </span>
                </div>
              </div>

              <div className="h-2 w-full bg-neutral-100 rounded-full overflow-hidden">
                <div
                  className="h-full bg-primary-500 rounded-full transition-all duration-500"
                  style={{ width: `${item.adherencePercentage}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      </div>

      {/* 4. Attention Category Breakdown */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm space-y-3.5">
        <div className="flex items-center gap-2">
          <AlertTriangle className="h-4 w-4 text-danger" />
          <h3 className="text-sm sm:text-base font-semibold text-neutral-900">
            Risk & Takip Kategorileri
          </h3>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          {initialData.attentionCategories.map((cat) => (
            <div
              key={cat.category}
              className="bg-surface-alt rounded-2xl p-3.5 space-y-1.5 border border-neutral-100"
            >
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold text-neutral-800 truncate">{cat.category}</span>
                <span
                  className={cn(
                    'font-mono text-xs font-bold px-2 py-0.5 rounded-full text-white',
                    cat.colorClass
                  )}
                >
                  {cat.count}
                </span>
              </div>
              <p className="text-[11px] text-neutral-500 leading-relaxed">
                {cat.description}
              </p>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
