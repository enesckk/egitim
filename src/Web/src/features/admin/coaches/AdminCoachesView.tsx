import React, { useState, useMemo } from 'react';
import { Search, AlertTriangle, AlertCircle, RefreshCw } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialAdminCoachesData } from './mockData';
import { AdminCoachesViewModel } from './types';

export interface AdminCoachesViewProps {
  initialData?: AdminCoachesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminCoachesView: React.FC<AdminCoachesViewProps> = ({
  initialData = initialAdminCoachesData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [branchFilter, setBranchFilter] = useState('');

  const filteredCoaches = useMemo(() => {
    return initialData.coaches.filter((coach) => {
      if (
        searchTerm &&
        !coach.name.toLowerCase().includes(searchTerm.toLowerCase()) &&
        !coach.title.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      if (branchFilter && coach.branch !== branchFilter) return false;
      return true;
    });
  }, [initialData.coaches, searchTerm, branchFilter]);

  const branches = useMemo(
    () => [...new Set(initialData.coaches.map((c) => c.branch))],
    [initialData.coaches]
  );

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-44 w-full rounded-2xl" />
          <Skeleton className="h-44 w-full rounded-2xl" />
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
          title="Koç Listesi Yüklenemedi"
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
          Koç Yönetimi & Portföy
        </h1>
        <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
          Kurum bünyesindeki {filteredCoaches.length} aktif öğrenci koçu
        </p>
      </div>

      {/* 2. Search & Branch Filter */}
      <div className="flex flex-col sm:flex-row items-center gap-3">
        <div className="relative flex-1 w-full max-w-sm">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Koç adı veya unvan ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          />
        </div>

        <div className="flex items-center gap-2 self-start sm:self-center">
          <select
            value={branchFilter}
            onChange={(e) => setBranchFilter(e.target.value)}
            className="px-3 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          >
            <option value="">Tüm Şubeler</option>
            {branches.map((b) => (
              <option key={b} value={b}>
                {b} Şubesi
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* 3. Coaches Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filteredCoaches.map((coach) => (
          <div
            key={coach.id}
            className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm hover:border-neutral-200 transition-colors space-y-4"
          >
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-3">
                <div className="w-11 h-11 rounded-2xl bg-navy-50 text-navy-800 text-sm font-bold flex items-center justify-center flex-shrink-0">
                  {coach.initials}
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-neutral-900">{coach.name}</h3>
                  <p className="text-xs text-neutral-400">
                    {coach.title} • {coach.branch} Şubesi
                  </p>
                </div>
              </div>

              {coach.attentionStudentCount > 0 && (
                <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-danger-dark bg-danger-light border border-red-200 px-2.5 py-1 rounded-full flex-shrink-0">
                  <AlertTriangle className="h-3 w-3 text-danger" />
                  {coach.attentionStudentCount} Takip
                </span>
              )}
            </div>

            <div className="grid grid-cols-3 gap-2 bg-surface-alt p-3 rounded-xl text-center">
              <div>
                <span className="text-[10px] text-neutral-400 block font-medium">Öğrenci</span>
                <span className="font-mono text-sm font-bold text-neutral-900">
                  {coach.assignedStudentCount} / {coach.portfolioCapacity}
                </span>
              </div>
              <div>
                <span className="text-[10px] text-neutral-400 block font-medium">Plan Uyumu</span>
                <span
                  className={cn(
                    'font-mono text-sm font-bold',
                    coach.averageAdherencePercentage >= 80
                      ? 'text-success'
                      : coach.averageAdherencePercentage >= 70
                      ? 'text-warning-dark'
                      : 'text-danger'
                  )}
                >
                  %{coach.averageAdherencePercentage}
                </span>
              </div>
              <div>
                <span className="text-[10px] text-neutral-400 block font-medium">Haftalık Ort.</span>
                <span className="font-mono text-sm font-bold text-neutral-900">
                  {coach.averageWeeklyStudyHours} sa
                </span>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
