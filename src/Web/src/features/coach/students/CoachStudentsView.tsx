import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, Users, AlertCircle, RefreshCw } from 'lucide-react';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { StudentDirectoryItem } from './components/StudentDirectoryItem';
import { initialCoachStudentsData } from './mockData';
import { AssignedStudentDetail, CoachStudentsViewModel } from './types';

export interface CoachStudentsViewProps {
  initialData?: CoachStudentsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const CoachStudentsView: React.FC<CoachStudentsViewProps> = ({
  initialData = initialCoachStudentsData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [gradeFilter, setGradeFilter] = useState('');

  const handleSelectStudent = (student: AssignedStudentDetail) => {
    navigate(`/coach/students/${student.id}`);
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="space-y-3">
          <Skeleton className="h-20 w-full rounded-2xl" />
          <Skeleton className="h-20 w-full rounded-2xl" />
          <Skeleton className="h-20 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenciler Yüklenemedi">
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

  const filteredStudents = initialData.students.filter((student) => {
    const matchesSearch =
      student.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      student.field.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesStatus = !statusFilter || student.status === statusFilter;
    const matchesGrade = !gradeFilter || student.grade.includes(gradeFilter);
    return matchesSearch && matchesStatus && matchesGrade;
  });

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* 1. Header & Title */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Atanmış Öğrenciler
          </h1>
          <p className="text-xs text-neutral-400 mt-0.5">
            Koçluk portföyünüzdeki toplam {initialData.students.length} öğrencinin güncel durumu
          </p>
        </div>
      </div>

      {/* 2. Search & Filter Bar */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-3 sm:p-4 flex flex-col sm:flex-row items-center gap-2.5 shadow-soft-sm">
        <div className="flex-1 w-full relative">
          <Input
            placeholder="Öğrenci adı veya alan ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
            className="text-xs min-h-[38px]"
          />
        </div>

        <div className="flex items-center gap-2 w-full sm:w-auto">
          <Select
            options={[
              { value: 'takipte', label: 'Takipte (İyi)' },
              { value: 'dikkat', label: 'Dikkat Gerektiren' },
              { value: 'kritik', label: 'Kritik Durum' },
            ]}
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            placeholder="Tüm Durumlar"
            className="text-xs min-h-[38px] py-1.5 pl-2.5 pr-7 flex-1 sm:w-40"
          />

          <Select
            options={[
              { value: '10', label: '10. Sınıf' },
              { value: '11', label: '11. Sınıf' },
              { value: '12', label: '12. Sınıf' },
            ]}
            value={gradeFilter}
            onChange={(e) => setGradeFilter(e.target.value)}
            placeholder="Tüm Sınıflar"
            className="text-xs min-h-[38px] py-1.5 pl-2.5 pr-7 flex-1 sm:w-36"
          />
        </div>
      </div>

      {/* 3. Students List */}
      {filteredStudents.length > 0 ? (
        <div className="space-y-2.5">
          {filteredStudents.map((student) => (
            <StudentDirectoryItem
              key={student.id}
              student={student}
              onClick={handleSelectStudent}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<Users className="h-6 w-6 text-neutral-400" />}
          title="Öğrenci Bulunamadı"
          description="Arama kriterlerinize veya seçilen filtrelere uygun öğrenci kaydı yok."
        />
      )}
    </div>
  );
};
