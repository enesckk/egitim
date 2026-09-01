import React, { useState, useMemo } from 'react';
import { Search, GraduationCap, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialAdminTeachersData } from './mockData';
import { AdminTeachersViewModel } from './types';

export interface AdminTeachersViewProps {
  initialData?: AdminTeachersViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminTeachersView: React.FC<AdminTeachersViewProps> = ({
  initialData = initialAdminTeachersData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [searchTerm, setSearchTerm] = useState('');
  const [branchFilter, setBranchFilter] = useState('');

  const filteredTeachers = useMemo(() => {
    return initialData.teachers.filter((teacher) => {
      if (
        searchTerm &&
        !teacher.name.toLowerCase().includes(searchTerm.toLowerCase()) &&
        !teacher.subject.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      if (branchFilter && teacher.branch !== branchFilter) return false;
      return true;
    });
  }, [initialData.teachers, searchTerm, branchFilter]);

  const branches = useMemo(
    () => [...new Set(initialData.teachers.map((t) => t.branch))],
    [initialData.teachers]
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
          title="Öğretmen Listesi Yüklenemedi"
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
          Öğretmenler & Zümre Yönetimi
        </h1>
        <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
          Kurum bünyesindeki {filteredTeachers.length} aktif branş öğretmeni
        </p>
      </div>

      {/* 2. Search & Branch Filter */}
      <div className="flex flex-col sm:flex-row items-center gap-3">
        <div className="relative flex-1 w-full max-w-sm">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Öğretmen adı veya branş ara..."
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

      {/* 3. Teachers Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filteredTeachers.map((teacher) => (
          <div
            key={teacher.id}
            className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm hover:border-neutral-200 transition-colors space-y-4"
          >
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-3">
                <div className="w-11 h-11 rounded-2xl bg-primary-50 text-primary-700 text-sm font-bold flex items-center justify-center flex-shrink-0">
                  {teacher.initials}
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-neutral-900">{teacher.name}</h3>
                  <p className="text-xs text-neutral-500 font-medium">
                    {teacher.subject} • {teacher.branch} Şubesi
                  </p>
                </div>
              </div>

              <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full">
                {teacher.totalStudentCount} Öğrenci
              </span>
            </div>

            <div className="space-y-1.5 pt-1">
              <span className="text-[11px] text-neutral-400 font-semibold uppercase tracking-wider block">
                Atanmış Şubeler:
              </span>
              <div className="flex flex-wrap gap-1.5">
                {teacher.assignedClasses.map((cls) => (
                  <span
                    key={cls}
                    className="inline-flex items-center gap-1 text-xs bg-surface-alt border border-neutral-200 text-neutral-700 px-2.5 py-1 rounded-lg"
                  >
                    <GraduationCap className="h-3 w-3 text-neutral-400" />
                    {cls}
                  </span>
                ))}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
