import React from 'react';
import { useNavigate } from 'react-router-dom';
import { BookOpen, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { AcademicGapsCard } from './components/AcademicGapsCard';
import { StudentNetsCard } from './components/StudentNetsCard';
import { initialTeacherDashboardData } from './mockData';
import { TeacherDashboardViewModel } from './types';

export interface TeacherDashboardViewProps {
  initialData?: TeacherDashboardViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherDashboardView: React.FC<TeacherDashboardViewProps> = ({
  initialData = initialTeacherDashboardData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-4">
          <div className="xl:col-span-2 space-y-3">
            <Skeleton className="h-28 w-full rounded-2xl" />
            <Skeleton className="h-28 w-full rounded-2xl" />
            <Skeleton className="h-28 w-full rounded-2xl" />
          </div>
          <Skeleton className="h-72 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
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

  const todayStr = new Date().toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* 1. Header Greeting */}
      <div>
        <p className="text-neutral-400 text-xs sm:text-sm capitalize font-sans">
          {todayStr}
        </p>
        <h1 className="font-serif text-2xl sm:text-3xl lg:text-4xl text-neutral-900 leading-tight mt-0.5">
          Günaydın, {initialData.teacherName}
        </h1>
        <div className="flex items-center gap-2 text-xs sm:text-sm text-neutral-500 mt-1">
          <BookOpen className="h-4 w-4 text-primary-600 flex-shrink-0" />
          <span>
            {initialData.assignedSubjects.join(' · ')} · {initialData.totalAssignedStudents} Atanmış Öğrenci ({initialData.totalAssignedClasses} Şube)
          </span>
        </div>
      </div>

      {/* 2. Main 2-Column Content Layout (Matching Figma) */}
      <div className="grid grid-cols-1 xl:grid-cols-3 gap-4 sm:gap-6">
        {/* Left Column: Academic Gaps */}
        <div className="xl:col-span-2 space-y-4">
          <AcademicGapsCard
            gaps={initialData.academicGaps}
            onSelectStudent={(studentId) => navigate(`/teacher/students/${studentId}`)}
          />
        </div>

        {/* Right Column: Student Net Overview & Scope Note */}
        <div className="space-y-4">
          <StudentNetsCard
            students={initialData.studentNets}
            onSelectStudent={(studentId) => navigate(`/teacher/students/${studentId}`)}
          />
        </div>
      </div>
    </div>
  );
};
