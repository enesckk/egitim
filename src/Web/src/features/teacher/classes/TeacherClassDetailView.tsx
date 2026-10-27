import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  Users,
  BookOpen,
  User,
  AlertCircle,
  RefreshCw,
  ChevronRight,
  TrendingUp,
  GraduationCap,
  Sparkles,
} from 'lucide-react';
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
      studentNumber: s.studentNumber || null,
      name: s.fullName,
      initials,
      mathNet: null,
      physicsNet: null,
      status: 'takipte',
      trend: 'stable',
    };
  });

  const teacherNames = (dto.teachers || []).map((t) => t.fullName).filter(Boolean);
  const teacherTitles = (dto.teachers || []).map((t) => t.title).filter(Boolean);
  const subjectDisplay = teacherTitles.length > 0 ? teacherTitles.join(', ') : 'Branş Dersi';

  return {
    id: dto.id,
    name: dto.name,
    grade: dto.gradeLevel ? `${dto.gradeLevel}. Sınıf` : '12. Sınıf',
    branch: dto.branch || 'Genel Şube',
    teacherName: teacherNames.length > 0 ? teacherNames.join(', ') : null,
    subject: subjectDisplay,
    studentCount: students.length,
    averageNet: null,
    attentionRequiredCount: 0,
    students,
    topicMasteries: [],
    recentExamTitle: null,
    recentExamAverageNet: null,
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
      <div className="w-full max-w-6xl mx-auto space-y-5 select-none" data-testid="teacher-class-detail-loading">
        <Skeleton className="h-9 w-32 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
          <Skeleton className="h-80 w-full rounded-2xl lg:col-span-2" />
          <Skeleton className="h-80 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError || !cls) {
    return (
      <div className="w-full max-w-6xl mx-auto py-8 space-y-4 select-none" data-testid="teacher-class-detail-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Sınıf Bulunamadı">
          {activeError || 'İstenen şube kaydına ulaşılamadı veya yetkiniz bulunmuyor.'}
        </Alert>
        <div className="flex justify-center gap-3 pt-2">
          <Button variant="secondary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
          <Button variant="primary" size="sm" onClick={() => navigate('/teacher/classes')} leftIcon={<ArrowLeft className="h-4 w-4" />}>
            Sınıflara Dön
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="teacher-class-detail-view">
      {/* Back button */}
      <div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate('/teacher/classes')}
          leftIcon={<ArrowLeft className="h-4 w-4 text-neutral-600" />}
          className="text-neutral-600 hover:text-neutral-900 -ml-2 font-medium"
        >
          Sınıflarıma Dön
        </Button>
      </div>

      {/* Header Banner - Premium Navy / Ivory Card */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-6 shadow-soft-sm">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 sm:gap-5">
          <div className="space-y-2">
            <div className="flex items-center gap-2.5 flex-wrap">
              <h1 className="font-serif text-2xl sm:text-3xl font-bold text-neutral-900 tracking-tight">
                {cls.name}
              </h1>
              <span className="text-xs font-bold px-3 py-1 rounded-full bg-primary-50 text-primary-700 border border-primary-200">
                {cls.grade}
              </span>
              <span className="text-xs font-semibold px-2.5 py-1 rounded-full bg-neutral-100 text-neutral-600">
                {cls.branch}
              </span>
            </div>
            <div className="flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4 text-xs sm:text-sm text-neutral-600">
              <div className="flex items-center gap-1.5">
                <BookOpen className="h-4 w-4 text-primary-600 flex-shrink-0" />
                <span>Branş: <strong className="text-neutral-800 font-medium">{cls.subject}</strong></span>
              </div>
              {cls.teacherName && (
                <div className="flex items-center gap-1.5 text-neutral-500">
                  <User className="h-3.5 w-3.5 text-neutral-400 flex-shrink-0" />
                  <span>Öğretmen: <strong className="text-neutral-700 font-medium">{cls.teacherName}</strong></span>
                </div>
              )}
            </div>
          </div>

          {/* Quick Metrics */}
          <div className="grid grid-cols-2 gap-2.5 sm:gap-3 lg:flex lg:items-center lg:gap-4 border-t lg:border-t-0 lg:border-l border-neutral-200/80 pt-3.5 sm:pt-4 lg:pt-0 lg:pl-6">
            <div className="bg-[#F8F7F3] p-3 rounded-xl border border-neutral-200/80 min-w-[120px]">
              <span className="text-[10px] uppercase font-bold text-neutral-500 tracking-wider block">
                Kayıtlı Öğrenci
              </span>
              <div className="flex items-center gap-1.5 mt-1">
                <Users className="h-4 w-4 text-primary-600" />
                <span className="font-mono text-lg font-bold text-neutral-900">
                  {cls.studentCount}
                </span>
              </div>
            </div>

            <div className="bg-[#F8F7F3] p-3 rounded-xl border border-neutral-200/80 min-w-[140px]">
              <span className="text-[10px] uppercase font-bold text-neutral-500 tracking-wider block">
                Ortalama Net
              </span>
              <span className="text-xs sm:text-sm font-semibold text-neutral-700 mt-1 block truncate">
                {cls.averageNet != null ? `${cls.averageNet} net` : 'Henüz sınav verisi yok'}
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Main Grid: Student Roster & Topic Mastery */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4 sm:gap-5">
        {/* Left Column (2 Cols on LG): Sınıf Öğrencileri */}
        <div className="lg:col-span-2 bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-sm space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-neutral-100 pb-3.5">
            <div>
              <h2 className="text-base font-bold text-neutral-900 flex items-center gap-2">
                <Users className="h-4 w-4 text-primary-600" />
                Sınıf Öğrencileri ({cls.students.length})
              </h2>
              <p className="text-xs text-neutral-500 mt-0.5">
                Bireysel karne ve soru bazlı akademik ilerleme için öğrenciye tıklayın
              </p>
            </div>
            <span className="text-[11px] font-medium text-neutral-500 bg-[#F8F7F3] px-2.5 py-1 rounded-lg border border-neutral-200/70 self-start sm:self-auto">
              Aktif Kadro
            </span>
          </div>

          <div className="space-y-2.5">
            {cls.students.length === 0 ? (
              <div className="text-center py-10">
                <GraduationCap className="h-8 w-8 text-neutral-400 mx-auto mb-2" />
                <p className="text-sm font-medium text-neutral-700">Bu sınıfa henüz öğrenci kaydedilmemiş.</p>
                <p className="text-xs text-neutral-500 mt-1">Öğrenci atamaları kurum yöneticisi tarafından gerçekleştirilir.</p>
              </div>
            ) : (
              cls.students.map((student) => (
                <div
                  key={student.id}
                  onClick={() => navigate(`/teacher/students/${student.studentId}`)}
                  className="flex flex-col sm:flex-row sm:items-center justify-between p-3.5 sm:p-4 rounded-xl hover:bg-neutral-50/90 transition-all cursor-pointer border border-neutral-200/70 hover:border-primary-300 gap-3 group"
                >
                  <div className="flex items-center gap-3.5 min-w-0">
                    <div className="w-10 h-10 rounded-full bg-[#14233E] text-white text-xs font-bold flex items-center justify-center flex-shrink-0 shadow-sm">
                      {student.initials}
                    </div>
                    <div className="min-w-0">
                      <p className="text-sm font-bold text-neutral-900 truncate group-hover:text-primary-700 transition-colors">
                        {student.name}
                      </p>
                      {student.studentNumber && (
                        <div className="flex items-center gap-2 text-xs text-neutral-500 mt-0.5">
                          <span>Öğrenci No: <strong className="font-mono text-neutral-700">{student.studentNumber}</strong></span>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="flex items-center justify-between sm:justify-end gap-3 flex-shrink-0 border-t sm:border-t-0 pt-2 sm:pt-0 border-neutral-100">
                    <span className="text-xs font-semibold text-primary-700 group-hover:underline flex items-center gap-1">
                      Öğrenci Karnesi
                      <ChevronRight className="h-4 w-4 transform group-hover:translate-x-1 transition-transform" />
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Right Column (1 Col on LG): Konu & Kazanım Hakimiyeti */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-sm space-y-4">
          <div className="border-b border-neutral-100 pb-3.5">
            <h2 className="text-base font-bold text-neutral-900 flex items-center gap-2">
              <TrendingUp className="h-4 w-4 text-primary-600" />
              Konu & Kazanım Hakimiyeti
            </h2>
            <p className="text-xs text-neutral-500 mt-0.5">
              Şube bazında deneme sınavı analizleri
            </p>
          </div>

          <div>
            {cls.topicMasteries.length === 0 ? (
              <div className="bg-[#F8F7F3] rounded-xl p-4 sm:p-5 border border-neutral-200/70 text-center space-y-2">
                <Sparkles className="h-6 w-6 text-neutral-400 mx-auto" />
                <p className="text-xs font-semibold text-neutral-700">
                  Bu şube için henüz konu hakimiyet verisi bulunmuyor.
                </p>
                <p className="text-[11px] text-neutral-500 leading-relaxed">
                  Öğrenciler deneme sınavlarına girdikçe veya optik formlar okutuldukça konu bazlı başarı oranları burada listelenecektir.
                </p>
              </div>
            ) : (
              <div className="space-y-3">
                {cls.topicMasteries.map((topic) => (
                  <div key={topic.topic} className="space-y-1.5 bg-[#F8F7F3] p-3 rounded-xl border border-neutral-200/70">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-semibold text-neutral-900">{topic.topic}</span>
                      <span className="font-mono font-bold text-primary-700">%{topic.masteryPercentage}</span>
                    </div>
                    <div className="h-2 w-full bg-neutral-200 rounded-full overflow-hidden">
                      <div
                        className="h-full bg-primary-600 rounded-full"
                        style={{ width: `${topic.masteryPercentage}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
