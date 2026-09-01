import React, { useState, useMemo } from 'react';
import { Search, GraduationCap, Users, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialAdminClassesData } from './mockData';
import { AdminClassesViewModel } from './types';

export interface AdminClassesViewProps {
  initialData?: AdminClassesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminClassesView: React.FC<AdminClassesViewProps> = ({
  initialData = initialAdminClassesData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [gradeFilter, setGradeFilter] = useState('');

  const filteredClasses = useMemo(() => {
    return initialData.classes.filter((cls) => {
      if (
        searchTerm &&
        !cls.name.toLowerCase().includes(searchTerm.toLowerCase()) &&
        !cls.field.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      if (gradeFilter && !cls.grade.includes(gradeFilter)) return false;
      return true;
    });
  }, [initialData.classes, searchTerm, gradeFilter]);

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-48 w-full rounded-2xl" />
          <Skeleton className="h-48 w-full rounded-2xl" />
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
          title="Sınıf Listesi Yüklenemedi"
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
          Sınıflar & Gruplar
        </h1>
        <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
          Kurum bünyesindeki {filteredClasses.length} aktif şube ve öğrenci grupları
        </p>
      </div>

      {/* 2. Search & Grade Filter */}
      <div className="flex flex-col sm:flex-row items-center gap-3">
        <div className="relative flex-1 w-full max-w-sm">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Sınıf veya alan ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          />
        </div>

        <div className="flex items-center gap-2 self-start sm:self-center">
          <select
            value={gradeFilter}
            onChange={(e) => setGradeFilter(e.target.value)}
            className="px-3 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          >
            <option value="">Tüm Kademeler</option>
            <option value="9">9. Sınıf</option>
            <option value="10">10. Sınıf</option>
            <option value="11">11. Sınıf</option>
            <option value="12">12. Sınıf</option>
          </select>
        </div>
      </div>

      {/* 3. Class Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filteredClasses.map((cls) => (
          <div
            key={cls.id}
            className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm hover:border-neutral-200 transition-colors space-y-4"
          >
            <div className="flex items-start justify-between gap-2">
              <div>
                <h3 className="text-base font-bold text-neutral-900">{cls.name}</h3>
                <p className="text-xs text-neutral-400 mt-0.5">
                  {cls.grade} • {cls.field}
                </p>
              </div>
              <span className="inline-flex items-center gap-1.5 text-xs font-semibold bg-neutral-100 text-neutral-700 px-2.5 py-1 rounded-full">
                <Users className="h-3.5 w-3.5 text-neutral-500" />
                {cls.studentCount} Öğrenci
              </span>
            </div>

            <div className="grid grid-cols-2 gap-2 bg-surface-alt p-3 rounded-xl">
              <div>
                <span className="text-[10px] text-neutral-400 block font-medium">Sınıf Net Ort.</span>
                <span className="font-mono text-base font-bold text-primary-700">
                  {cls.averageNet} net
                </span>
              </div>
              <div>
                <span className="text-[10px] text-neutral-400 block font-medium">Plan Uyumu</span>
                <span className="font-mono text-base font-bold text-success">
                  %{cls.averageAdherencePercentage}
                </span>
              </div>
            </div>

            <div className="space-y-1.5 pt-1">
              <span className="text-[11px] text-neutral-400 font-semibold uppercase tracking-wider block">
                Zümre Öğretmenleri:
              </span>
              <div className="space-y-1">
                {cls.assignedTeachers.map((t, idx) => (
                  <div key={idx} className="flex items-center gap-1.5 text-xs text-neutral-600">
                    <GraduationCap className="h-3.5 w-3.5 text-neutral-400 flex-shrink-0" />
                    <span className="truncate">{t}</span>
                  </div>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
