import React from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Users, BookOpen, AlertTriangle, CheckCircle2, XCircle, AlertCircle, RefreshCw, ChevronRight } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialTeacherClassesData } from './mockData';
import { TeacherClassDetail } from './types';

export interface TeacherClassDetailViewProps {
  initialClasses?: TeacherClassDetail[];
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherClassDetailView: React.FC<TeacherClassDetailViewProps> = ({
  initialClasses = initialTeacherClassesData.classes,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const { classId } = useParams<{ classId: string }>();
  const navigate = useNavigate();

  const cls = initialClasses.find((c) => c.id === classId) || initialClasses[0];

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
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
  if (errorMessage || !cls) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Sınıf Bulunamadı">
          {errorMessage || 'İstenen şube kaydına ulaşılamadı veya yetkiniz bulunmuyor.'}
        </Alert>
        <div className="flex justify-center gap-2 pt-2">
          {onRetry && (
            <Button variant="secondary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          )}
          <Button variant="primary" size="sm" onClick={() => navigate('/teacher/classes')} leftIcon={<ArrowLeft className="h-4 w-4" />}>
            Sınıf Listesine Dön
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Back Button */}
      <div>
        <button
          type="button"
          onClick={() => navigate('/teacher/classes')}
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-neutral-500 hover:text-primary-600 transition-colors py-1 px-2 rounded-lg hover:bg-neutral-100"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Sınıf Listesine Dön
        </button>
      </div>

      {/* Class Identity Header */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-6 shadow-soft-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="font-serif text-xl sm:text-2xl font-bold text-neutral-900 leading-tight">
              {cls.name}
            </h1>
            <span className="text-xs font-semibold bg-primary-50 text-primary-700 px-2.5 py-0.5 rounded-full border border-primary-100">
              {cls.branch}
            </span>
          </div>
          <div className="flex items-center gap-3 text-xs sm:text-sm text-neutral-500 mt-1">
            <span className="flex items-center gap-1">
              <BookOpen className="h-3.5 w-3.5 text-primary-600" />
              {cls.subject}
            </span>
            <span>•</span>
            <span className="flex items-center gap-1">
              <Users className="h-3.5 w-3.5 text-neutral-400" />
              {cls.studentCount} Öğrenci
            </span>
          </div>
        </div>

        <div className="flex items-center gap-3 self-start sm:self-center">
          <div className="bg-surface-alt rounded-2xl p-3 border border-neutral-100 text-right">
            <span className="text-[10px] uppercase tracking-wider text-neutral-400 font-semibold block">
              {cls.recentExamTitle} Ortalaması
            </span>
            <span className="font-mono text-lg font-bold text-neutral-900">
              {cls.recentExamAverageNet} net
            </span>
          </div>
        </div>
      </div>

      {/* Grid: Students in Class vs Topic Mastery */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 sm:gap-6">
        {/* Left Column: Sınıf Öğrencileri */}
        <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm space-y-3">
          <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
            <h3 className="text-sm font-semibold text-neutral-900 flex items-center gap-1.5">
              <Users className="h-4 w-4 text-neutral-500" />
              Şube Öğrenci Takibi ({cls.students.length})
            </h3>
            <span className="text-[11px] text-neutral-400 font-mono">
              Netler (Mat / Fiz)
            </span>
          </div>

          <div className="divide-y divide-neutral-50 max-h-[420px] overflow-y-auto">
            {cls.students.map((student) => (
              <div
                key={student.id}
                onClick={() => navigate(`/teacher/students/${student.studentId}`)}
                className="py-3 flex items-center justify-between gap-3 hover:bg-neutral-50/70 transition-colors cursor-pointer rounded-xl px-2 -mx-2"
              >
                <div className="flex items-center gap-2.5 min-w-0">
                  <div className="w-8 h-8 rounded-xl bg-navy-900 text-white font-semibold text-xs flex items-center justify-center flex-shrink-0">
                    {student.initials}
                  </div>
                  <div className="min-w-0">
                    <p className="text-xs sm:text-sm font-semibold text-neutral-900 truncate">
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
                  <div className="text-right font-mono text-xs">
                    {student.mathNet !== null && (
                      <span className="text-neutral-800 font-semibold block">
                        Mat: {student.mathNet}
                      </span>
                    )}
                    {student.physicsNet !== null && (
                      <span className="text-neutral-600 block">
                        Fiz: {student.physicsNet}
                      </span>
                    )}
                  </div>

                  <div>
                    {student.status === 'takipte' && <CheckCircle2 className="h-3.5 w-3.5 text-success" />}
                    {student.status === 'dikkat' && <AlertTriangle className="h-3.5 w-3.5 text-warning" />}
                    {student.status === 'kritik' && <XCircle className="h-3.5 w-3.5 text-danger" />}
                  </div>

                  <ChevronRight className="h-3.5 w-3.5 text-neutral-400" />
                </div>
              </div>
            ))}
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
