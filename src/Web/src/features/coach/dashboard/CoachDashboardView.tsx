import React from 'react';
import { useNavigate } from 'react-router-dom';
import { AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { AttentionStudentsCard } from './components/AttentionStudentsCard';
import { TodaySessionsCard } from './components/TodaySessionsCard';
import { PendingActionsCard } from './components/PendingActionsCard';
import { PortfolioContextCard } from './components/PortfolioContextCard';
import { initialCoachDashboardData } from './mockData';
import { AttentionStudentItem, CoachDashboardViewModel, PendingActionItem } from './types';

export interface CoachDashboardViewProps {
  initialData?: CoachDashboardViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const CoachDashboardView: React.FC<CoachDashboardViewProps> = ({
  initialData = initialCoachDashboardData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();

  const handleStudentAction = (student: AttentionStudentItem) => {
    navigate(`/coach/students/${student.studentId}?tab=${student.targetTab || 'overview'}`);
  };

  const handleJoinSession = () => {
    navigate('/coach/meetings');
  };

  const handleReviewAction = (action: PendingActionItem) => {
    navigate(`/coach/students/${action.studentId}`);
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="lg:col-span-2 space-y-4">
            <Skeleton className="h-56 w-full rounded-2xl" />
            <Skeleton className="h-44 w-full rounded-2xl" />
          </div>
          <div className="space-y-4">
            <Skeleton className="h-40 w-full rounded-2xl" />
            <Skeleton className="h-52 w-full rounded-2xl" />
          </div>
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Genel Bakış Yüklenemedi">
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* 1. Greeting & Date (Figma CoachDashboard.tsx) */}
      <div className="space-y-1">
        <p className="text-xs text-neutral-400">{initialData.dateStr}</p>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          İyi günler, {initialData.coachName.split(' ')[0]}
        </h1>
      </div>

      {/* 2. Main Dashboard Grid (Figma CoachDashboard.tsx layout) */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 sm:gap-6">
        {/* Left 2 Columns: Attention Students + Today Sessions */}
        <div className="lg:col-span-2 space-y-4 sm:space-y-6">
          {/* Attention required */}
          <AttentionStudentsCard
            students={initialData.attentionStudents}
            onActionClick={handleStudentAction}
          />

          {/* Today's sessions */}
          <TodaySessionsCard
            sessions={initialData.todaySessions}
            onViewAllSessions={() => navigate('/coach/meetings')}
            onJoinSession={handleJoinSession}
          />
        </div>

        {/* Right 1 Column: Pending Actions + Portfolio Context */}
        <div className="space-y-4 sm:space-y-6">
          {/* Pending actions */}
          <PendingActionsCard
            actions={initialData.pendingActions}
            onReviewAction={handleReviewAction}
          />

          {/* Portfolio context */}
          <PortfolioContextCard
            summary={initialData.portfolioSummary}
            onNavigateToStudents={() => navigate('/coach/students')}
          />
        </div>
      </div>
    </div>
  );
};
