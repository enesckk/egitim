import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, GraduationCap, AlertCircle, RefreshCw } from 'lucide-react';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { ClassCard } from './components/ClassCard';
import { initialTeacherClassesData } from './mockData';
import { TeacherClassesViewModel } from './types';

export interface TeacherClassesViewProps {
  initialData?: TeacherClassesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherClassesView: React.FC<TeacherClassesViewProps> = ({
  initialData = initialTeacherClassesData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');
  const [gradeFilter, setGradeFilter] = useState('');

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Skeleton className="h-48 w-full rounded-2xl" />
          <Skeleton className="h-48 w-full rounded-2xl" />
          <Skeleton className="h-48 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Sınıflar Yüklenemedi">
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

  const filteredClasses = initialData.classes.filter((cls) => {
    const matchesSearch =
      cls.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      cls.branch.toLowerCase().includes(searchQuery.toLowerCase()) ||
      cls.subject.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesGrade = !gradeFilter || cls.grade.startsWith(gradeFilter);

    return matchesSearch && matchesGrade;
  });

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Header */}
      <div>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Atanmış Sınıflar & Gruplar
        </h1>
        <p className="text-xs text-neutral-400 mt-0.5">
          Ders verdiğiniz şubelerin akademik başarı, net ortalamaları ve konu kazanım takibi
        </p>
      </div>

      {/* Filter Bar */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-2.5">
        <div className="sm:col-span-2">
          <Input
            placeholder="Sınıf adı, alan veya ders ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
          />
        </div>

        <Select
          options={[
            { value: '12', label: '12. Sınıflar (YKS)' },
            { value: '11', label: '11. Sınıflar' },
            { value: '10', label: '10. Sınıflar' },
            { value: '9', label: '9. Sınıflar' },
          ]}
          value={gradeFilter}
          onChange={(e) => setGradeFilter(e.target.value)}
          placeholder="Tüm Kademeler"
        />
      </div>

      {/* Classes Grid */}
      {filteredClasses.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {filteredClasses.map((cls) => (
            <ClassCard
              key={cls.id}
              cls={cls}
              onSelect={(classId) => navigate(`/teacher/classes/${classId}`)}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<GraduationCap className="h-6 w-6 text-neutral-400" />}
          title="Sınıf Bulunamadı"
          description="Arama kriterlerinize uygun atanmış şube veya grup kaydı bulunmuyor."
        />
      )}
    </div>
  );
};
