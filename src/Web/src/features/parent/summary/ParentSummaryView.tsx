import React from 'react';
import { AlertCircle, RefreshCw, UserCheck } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { initialParentSummaryData } from './mockData';
import { ParentSummaryViewModel } from './types';
import { ChildHeroCard } from './components/ChildHeroCard';
import { WeeklyActivityCard } from './components/WeeklyActivityCard';
import { AcademicStatusCard } from './components/AcademicStatusCard';
import { UpcomingCard } from './components/UpcomingCard';

export interface ParentSummaryViewProps {
  initialData?: ParentSummaryViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const ParentSummaryView: React.FC<ParentSummaryViewProps> = ({
  initialData = initialParentSummaryData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-2xl mx-auto px-4 sm:px-6 py-6 space-y-4 select-none">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-24 w-full rounded-2xl" />
        <Skeleton className="h-44 w-full rounded-2xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-2xl mx-auto px-4 sm:px-6 py-10 space-y-4 select-none">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Öğrenci Özeti Yüklenemedi"
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

  // Empty State
  if (!initialData || !initialData.student) {
    return (
      <div className="max-w-2xl mx-auto px-4 sm:px-6 py-12 select-none">
        <EmptyState
          icon={<UserCheck className="h-8 w-8 text-neutral-400" />}
          title="Bağlı Öğrenci Kaydı Bulunamadı"
          description="Hesabınızla ilişkilendirilmiş aktif bir öğrenci kaydı görünmüyor. Lütfen kurumunuzla iletişime geçin."
        />
      </div>
    );
  }

  return (
    <div className="max-w-2xl mx-auto px-4 sm:px-6 py-6 select-none space-y-4">
      {/* 1. Page Header */}
      <div className="mb-2">
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Öğrenci Özeti
        </h1>
        <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
          Güncellendi: {initialData.lastUpdated}
        </p>
      </div>

      {/* 2. Child Identity Hero Card */}
      <ChildHeroCard student={initialData.student} />

      {/* 3. Weekly Summary Card */}
      <WeeklyActivityCard
        activity={initialData.weeklyActivity}
        planAdherencePercentage={initialData.planAdherencePercentage}
      />

      {/* 4. Academic Status Qualitative Cards */}
      <AcademicStatusCard items={initialData.academicStatus} />

      {/* 5. Upcoming Events */}
      <UpcomingCard events={initialData.upcomingEvents} />
    </div>
  );
};
