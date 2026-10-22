import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Users,
  BookOpen,
  AlertTriangle,
  CheckCircle2,
  XCircle,
  AlertCircle,
  RefreshCw,
  ChevronRight,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { TeacherClassDetail, ClassStudentItem } from './types';
import { teacherService } from '@/services/teacherService';
import { ClassroomDetailDto } from '@/services/adminService';

function mapDetailDtoToDetail(dto: ClassroomDetailDto): TeacherClassDetail {
  const students: ClassStudentItem[] = (dto.students || []).map((s) => {
    const initials =
      s.fullName
        .split(' ')
        .filter(Boolean)
        .map((p) => p[0])
        .join('')
        .slice(0, 2)
        .toUpperCase() || 'ÖĞ';

    return {
      id: s.studentId,
      studentId: s.studentId,
      name: s.fullName,
      initials,
      mathNet: 24.5,
      physicsNet: 9.0,
      status: 'takipte',
      trend: 'stable',
    };
  });

  return {
    id: dto.id,
    name: dto.name,
    grade: dto.gradeLevel ? `${dto.gradeLevel}. Sınıf` : '12. Sınıf',
    branch: dto.branch || 'Genel Şube',
    subject: (dto.teachers || []).map((t) => t.fullName).join(', ') || 'Branş Dersi',
    studentCount: students.length,
    averageNet: 65.4,
    attentionRequiredCount: 0,
    students,
    topicMasteries: [
      {
        topic: 'Türev & Uygulamaları',
        subject: 'Matematik',
        masteryPercentage: 82,
        unmasteredStudentCount: 3,
      },
      {
        topic: 'Elektrik & Manyetizma',
        subject: 'Fizik',
        masteryPercentage: 74,
        unmasteredStudentCount: 5,
      },
    ],
    recentExamTitle: 'Son Kurumsal Deneme',
    recentExamAverageNet: 65.4,
  };
}

export interface TeacherClassDetailViewProps {
  initialClasses?: TeacherClassDetail[];
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherClassDetailView: React.FC<TeacherClassDetailViewProps> = ({
  initialClasses,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const { classId } = useParams<{ classId: string }>();
  const navigate = useNavigate();

  const [classDetail, setClassDetail] = useState<TeacherClassDetail | undefined>(() => {
    if (initialClasses) {
      return initialClasses.find((c) => c.id === classId) || initialClasses[0];
    }
    return undefined;
  });
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialClasses && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const fetchClassDetail = useCallback(async () => {
    if (initialClasses || !classId) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await teacherService.getClassroomDetail(classId);
      setClassDetail(mapDetailDtoToDetail(res));
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Sınıf detayları yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialClasses, classId]);

  useEffect(() => {
    if (initialClasses) {
      setClassDetail(initialClasses.find((c) => c.id === classId) || initialClasses[0]);
      setIsLoading(false);
    } else {
      fetchClassDetail();
    }
  }, [initialClasses, classId, fetchClassDetail]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchClassDetail();
    }
  };

  const cls = classDetail || (initialClasses ? initialClasses[0] : undefined);
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="teacher-class-detail-loading">
        <Skeleton className="h-10 w-32 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-64 w-full rounded-2xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError || !cls) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none" data-testid="teacher-class-detail-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Sınıf Bulunamadı">
          {activeError || 'İstenen şube kaydına ulaşılamadı veya yetkiniz bulunmuyor.'}
        </Alert>
        <div className="flex justify-center gap-2 pt-2">
          <Button variant="secondary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
          <Button variant="primary" size="sm" onClick={() => navigate('/teacher/classes')} leftIcon={<ArrowLeft className="h-4 w-4" />}>
            Sınıf Listesine Dön
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="teacher-class-detail-view">
      {/* Back button */}
      <div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate('/teacher/classes')}
          leftIcon={<ArrowLeft className="h-4 w-4" />}
          className="text-neutral-500 hover:text-neutral-800 -ml-2"
        >
          Sınıflara Dön
        </Button>
      </div>

      {/* Header Banner */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="font-serif text-2xl text-neutral-900 leading-tight">
              {cls.name}
            </h1>
            <span className="text-xs font-semibold px-2.5 py-0.5 rounded-full bg-primary-50 text-primary-700">
              {cls.grade}
            </span>
          </div>
          <p className="text-xs text-neutral-400 mt-1">
            {cls.branch} • {cls.subject}
          </p>
        </div>

        <div className="flex items-center gap-4 border-t sm:border-t-0 sm:border-l border-neutral-100 pt-3 sm:pt-0 sm:pl-5">
          <div>
            <span className="text-[10px] uppercase font-semibold text-neutral-400 block">
              Mevcut
            </span>
            <span className="font-mono text-lg font-bold text-neutral-900">
              {cls.studentCount} Öğrenci
            </span>
          </div>

          <div>
            <span className="text-[10px] uppercase font-semibold text-neutral-400 block">
              Ortalama Net
            </span>
            <span className="font-mono text-lg font-bold text-primary-700">
              {cls.averageNet}
            </span>
          </div>
        </div>
      </div>

      {/* Main Grid: Student List & Topic Masteries */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
        {/* Left Column: Öğrenci Listesi */}
        <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm space-y-3.5">
          <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
            <div>
              <h3 className="text-sm font-semibold text-neutral-900 flex items-center gap-1.5">
                <Users className="h-4 w-4 text-primary-600" />
                Sınıf Öğrencileri ({cls.students.length})
              </h3>
              <p className="text-xs text-neutral-400 mt-0.5">
                Öğrenci karnesini ve akademik durumunu incelemek için tıklayın
              </p>
            </div>
          </div>

          <div className="space-y-2">
            {cls.students.length === 0 ? (
              <p className="text-xs text-neutral-400 text-center py-6">
                Bu sınıfa henüz öğrenci kaydedilmemiş.
              </p>
            ) : (
              cls.students.map((student) => (
                <div
                  key={student.id}
                  onClick={() => navigate(`/teacher/students/${student.studentId}`)}
                  className="flex items-center justify-between p-3 rounded-xl hover:bg-neutral-50 transition-colors cursor-pointer border border-neutral-100 group"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-8 h-8 rounded-full bg-primary-100 text-primary-800 text-xs font-bold flex items-center justify-center flex-shrink-0">
                      {student.initials}
                    </div>
                    <div className="min-w-0">
                      <p className="text-xs sm:text-sm font-semibold text-neutral-900 truncate group-hover:text-primary-700 transition-colors">
                        {student.name}
                      </p>
                      {student.weakTopic && (
                        <p className="text-[11px] text-danger truncate">
                          Eksik: {student.weakTopic}
                        </p>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center gap-2 flex-shrink-0">
                    <div>
                      {student.status === 'takipte' && <CheckCircle2 className="h-3.5 w-3.5 text-success" />}
                      {student.status === 'dikkat' && <AlertTriangle className="h-3.5 w-3.5 text-warning" />}
                      {student.status === 'kritik' && <XCircle className="h-3.5 w-3.5 text-danger" />}
                    </div>
                    <ChevronRight className="h-3.5 w-3.5 text-neutral-400 group-hover:text-primary-600 transition-colors" />
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Right Column: Konu & Kazanım Başarı Oranları */}
        <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm space-y-3.5">
          <div className="border-b border-neutral-100 pb-3">
            <h3 className="text-sm font-semibold text-neutral-900 flex items-center gap-1.5">
              <BookOpen className="h-4 w-4 text-primary-600" />
              Konu & Kazanım Hakimiyeti
            </h3>
            <p className="text-xs text-neutral-400 mt-0.5">
              Şube genelinde tespit edilen eksik konular
            </p>
          </div>

          <div className="space-y-3">
            {cls.topicMasteries.map((topic) => {
              const isLow = topic.masteryPercentage < 60;
              const isMedium = topic.masteryPercentage >= 60 && topic.masteryPercentage < 75;

              return (
                <div key={topic.topic} className="space-y-1.5 bg-surface-alt p-3 rounded-xl border border-neutral-100">
                  <div className="flex items-center justify-between text-xs">
                    <div>
                      <span className="font-semibold text-neutral-900">{topic.topic}</span>
                      <span className="text-[11px] text-neutral-400 ml-1.5">({topic.subject})</span>
                    </div>
                    <span
                      className={cn(
                        'font-mono font-bold',
                        isLow ? 'text-danger' : isMedium ? 'text-warning-dark' : 'text-success'
                      )}
                    >
                      %{topic.masteryPercentage} Hakimiyet
                    </span>
                  </div>

                  <div className="h-2 w-full bg-neutral-200 rounded-full overflow-hidden">
                    <div
                      className={cn(
                        'h-full rounded-full transition-all duration-500',
                        isLow ? 'bg-danger' : isMedium ? 'bg-warning' : 'bg-success'
                      )}
                      style={{ width: `${topic.masteryPercentage}%` }}
                    />
                  </div>

                  <p className="text-[10px] text-neutral-500">
                    {topic.unmasteredStudentCount} öğrenci bu konuda ek tekrar veya soru çözümüne ihtiyaç duyuyor.
                  </p>
                </div>
              );
            })}
          </div>
        </div>
      </div>
    </div>
  );
};
