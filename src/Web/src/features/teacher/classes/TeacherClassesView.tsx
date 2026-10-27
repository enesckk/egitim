import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Search, GraduationCap, AlertCircle, RefreshCw, Layers } from 'lucide-react';
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
  const teacherNames = (c.teachers || []).map((t) => t.fullName).filter(Boolean);
  const teacherTitles = (c.teachers || []).map((t) => t.title).filter(Boolean);
  const subjectDisplay = teacherTitles.length > 0 ? teacherTitles.join(', ') : 'Branş Dersi';

  return {
    id: c.id,
    name: c.name,
    grade: c.gradeLevel ? `${c.gradeLevel}. Sınıf` : '12. Sınıf',
    branch: c.branch || 'Genel Şube',
    teacherName: teacherNames.length > 0 ? teacherNames.join(', ') : null,
    subject: subjectDisplay,
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
      <div className="w-full max-w-6xl mx-auto space-y-5 select-none" data-testid="teacher-classes-loading">
        <div className="flex items-center justify-between">
          <Skeleton className="h-10 w-48 rounded-xl" />
          <Skeleton className="h-7 w-24 rounded-full" />
        </div>
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Skeleton className="h-52 w-full rounded-2xl" />
          <Skeleton className="h-52 w-full rounded-2xl" />
          <Skeleton className="h-52 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="w-full max-w-6xl mx-auto py-8 space-y-4" data-testid="teacher-classes-error">
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
      (cls.teacherName && cls.teacherName.toLowerCase().includes(searchQuery.toLowerCase())) ||
      cls.subject.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesGrade = !gradeFilter || cls.grade.startsWith(gradeFilter);

    return matchesSearch && matchesGrade;
  });

  const totalStudentsEnrolled = activeData.classes.reduce((sum, c) => sum + (c.studentCount || 0), 0);

  return (
    <div className="w-full max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="teacher-classes-view">
      {/* Premium Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 bg-white p-4 sm:p-6 rounded-2xl border border-neutral-200/80 shadow-soft-sm">
        <div>
          <div className="flex items-center gap-2.5 flex-wrap">
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-neutral-900 tracking-tight">
              Sınıflarım
            </h1>
            <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-primary-50 text-primary-700 border border-primary-200">
              <Layers className="h-3.5 w-3.5" />
              {activeData.classes.length} Atanmış Şube
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1">
            Ders verdiğiniz sınıfların öğrenci listesi, akademik takip durumu ve şube analizleri
          </p>
        </div>

        <div className="flex items-center gap-2.5 self-start sm:self-center bg-[#F8F7F3] px-3.5 py-2 rounded-xl border border-neutral-200/80 text-xs text-neutral-600">
          <GraduationCap className="h-4 w-4 text-primary-600 flex-shrink-0" />
          <span>Toplam Kayıtlı: <strong className="font-semibold text-neutral-900">{totalStudentsEnrolled} Öğrenci</strong></span>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white p-3.5 sm:p-4 rounded-2xl border border-neutral-200/80 shadow-soft-sm">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="sm:col-span-2">
            <Input
              placeholder="Sınıf adı, şube veya ders ara..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
              className="bg-[#F8F7F3] border-neutral-200 focus:bg-white"
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
            className="bg-[#F8F7F3] border-neutral-200"
          />
        </div>
      </div>

      {/* Classes Grid */}
      {filteredClasses.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4 sm:gap-5">
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
          icon={<GraduationCap className="h-8 w-8 text-neutral-400" />}
          title="Sınıf Bulunamadı"
          description="Arama kriterlerinize veya seçilen kademeye uygun atanmış şube kaydı bulunmuyor."
        />
      )}
    </div>
  );
};
