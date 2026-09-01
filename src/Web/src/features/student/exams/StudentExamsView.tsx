import React, { useState } from 'react';
import { Award, FileText, AlertCircle, RefreshCw } from 'lucide-react';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { LatestExamSummaryCard } from './components/LatestExamSummaryCard';
import { ExamListItemCard } from './components/ExamListItemCard';
import { ExamDetailModal } from './components/ExamDetailModal';
import { initialStudentExamsData } from './mockData';
import { StudentExamItem, StudentExamsViewModel } from './types';

export interface StudentExamsViewProps {
  initialData?: StudentExamsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const StudentExamsView: React.FC<StudentExamsViewProps> = ({
  initialData = initialStudentExamsData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [examTypeFilter, setExamTypeFilter] = useState<string>('');
  const [selectedExam, setSelectedExam] = useState<StudentExamItem | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState<boolean>(false);

  const handleOpenDetail = (exam: StudentExamItem) => {
    setSelectedExam(exam);
    setIsDetailOpen(true);
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-56 w-full rounded-2xl" />
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Skeleton className="h-20 w-full rounded-xl" />
          <Skeleton className="h-20 w-full rounded-xl" />
          <Skeleton className="h-20 w-full rounded-xl" />
          <Skeleton className="h-20 w-full rounded-xl" />
        </div>
        <Skeleton className="h-24 w-full rounded-2xl" />
        <Skeleton className="h-24 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Deneme Sonuçları Yüklenemedi">
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

  const filteredExams = initialData.exams.filter((ex) => {
    if (!examTypeFilter) return true;
    return ex.type === examTypeFilter;
  });

  return (
    <div className="max-w-5xl mx-auto select-none space-y-3.5 sm:space-y-4">
      {/* 1. Latest Exam Summary Card */}
      <LatestExamSummaryCard
        exam={initialData.latestExam}
        onViewAnalysis={handleOpenDetail}
      />

      {/* 2. Target & Average Stats Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 sm:gap-3">
        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">TYT Ortalama</span>
          <p className="font-mono text-lg sm:text-xl font-bold text-neutral-900 mt-0.5">
            {initialData.averageNetTYT} Net
          </p>
          <span className="text-[10px] text-neutral-400 font-mono">Hedef: {initialData.targetNetTYT}</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">AYT Ortalama</span>
          <p className="font-mono text-lg sm:text-xl font-bold text-neutral-900 mt-0.5">
            {initialData.averageNetAYT} Net
          </p>
          <span className="text-[10px] text-neutral-400 font-mono">Hedef: {initialData.targetNetAYT}</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">Girilmiş Deneme</span>
          <p className="font-mono text-lg sm:text-xl font-bold text-primary-700 mt-0.5">
            {initialData.exams.length} Adet
          </p>
          <span className="text-[10px] text-success font-medium">Aktif takipte</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">Genel Net Artışı</span>
          <p className="font-mono text-lg sm:text-xl font-bold text-success mt-0.5">
            +7.75 Net
          </p>
          <span className="text-[10px] text-neutral-400">Başlangıçtan bu yana</span>
        </div>
      </div>

      {/* 3. Filter Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 bg-white rounded-2xl border border-neutral-100 p-3 sm:px-4">
        <div className="flex items-center gap-2">
          <Award className="h-4 w-4 text-primary-600" />
          <h3 className="text-sm font-semibold text-neutral-900">
            Geçmiş Deneme Sınavları ({filteredExams.length})
          </h3>
        </div>

        <div className="flex items-center gap-2">
          <Select
            options={[
              { value: 'TYT', label: 'Yalnızca TYT' },
              { value: 'AYT', label: 'Yalnızca AYT' },
            ]}
            value={examTypeFilter}
            onChange={(e) => setExamTypeFilter(e.target.value)}
            placeholder="Tüm Sınav Türleri"
            className="text-xs min-h-[34px] py-1 pl-2.5 pr-7"
          />
        </div>
      </div>

      {/* 4. Exams List */}
      {filteredExams.length > 0 ? (
        <div className="space-y-2.5">
          {filteredExams.map((exam) => (
            <ExamListItemCard
              key={exam.id}
              exam={exam}
              onClick={handleOpenDetail}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<FileText className="h-6 w-6 text-neutral-400" />}
          title="Deneme Sınavı Bulunamadı"
          description="Seçilen sınav türü filtresine uygun sonuçlanmış deneme kaydı yok."
          action={
            examTypeFilter ? (
              <Button size="sm" variant="secondary" onClick={() => setExamTypeFilter('')}>
                Filtreyi Temizle
              </Button>
            ) : undefined
          }
        />
      )}

      {/* Exam Detail Modal */}
      <ExamDetailModal
        exam={selectedExam}
        isOpen={isDetailOpen}
        onClose={() => setIsDetailOpen(false)}
      />
    </div>
  );
};
