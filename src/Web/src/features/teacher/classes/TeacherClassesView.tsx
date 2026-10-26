import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, GraduationCap, AlertCircle, RefreshCw } from 'lucide-react';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { ClassCard } from './components/ClassCard';
import { TeacherClassesViewModel, TeacherClassDetail } from './types';
import { teacherService } from '@/services/teacherService';
import { ClassroomDto } from '@/services/adminService';

function mapClassroomToDetail(c: ClassroomDto): TeacherClassDetail {
  return {
    id: c.id,
    name: c.name,
    grade: c.gradeLevel ? `${c.gradeLevel}. Sınıf` : '12. Sınıf',
    branch: c.branch || 'Genel Şube',
    subject: (c.teachers || []).map((t) => t.fullName).join(', ') || 'Branş Dersi',
    studentCount: c.studentCount || 0,
    averageNet: null,
    attentionRequiredCount: 0,
    students: [],
    topicMasteries: [],
    recentExamTitle: null,
    recentExamAverageNet: null,
  };
}

export interface TeacherClassesViewProps {
  initialData?: TeacherClassesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherClassesView: React.FC<TeacherClassesViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [data, setData] = useState<TeacherClassesViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchQuery, setSearchQuery] = useState('');
  const [gradeFilter, setGradeFilter] = useState('');

  const fetchClasses = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await teacherService.listClassrooms();
      const items = (res || []).map(mapClassroomToDetail);
      setData({ classes: items });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Sınıflar yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchClasses();
    }
  }, [initialData, fetchClasses]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchClasses();
    }
  };

  const activeData = data || initialData || { classes: [] };
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="teacher-classes-loading">
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
  if (activeError) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4" data-testid="teacher-classes-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Sınıflar Yüklenemedi">
          {activeError}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const filteredClasses = activeData.classes.filter((cls) => {
    const matchesSearch =
      cls.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      cls.branch.toLowerCase().includes(searchQuery.toLowerCase()) ||
      cls.subject.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesGrade = !gradeFilter || cls.grade.startsWith(gradeFilter);

    return matchesSearch && matchesGrade;
  });

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="teacher-classes-view">
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
